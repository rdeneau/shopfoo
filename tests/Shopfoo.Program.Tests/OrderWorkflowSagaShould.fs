namespace Shopfoo.Product.Tests

open System
open System.Net
open Microsoft.Extensions.DependencyInjection
open Shopfoo.Common
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Tests.OrderContext
open Shopfoo.Product.Tests.OrderContext.Cmd
open Shopfoo.Product.Tests.OrderContext.Data
open Shopfoo.Product.Tests.OrderContext.Workflows
open Shopfoo.Program
open Shopfoo.Program.Dependencies
open Shopfoo.Program.Runner
open Shopfoo.Program.Tests.Helpers
open Shopfoo.Program.Tests.Mocks
open Swensen.Unquote
open TUnit.Core

type OrderWorkflowSagaShould() =
    let services = ServiceCollection().AddProgramMocks()
    let serviceProvider = services.BuildServiceProvider()
    let workflowRunnerFactory = serviceProvider.GetRequiredService<IWorkflowRunnerFactory>()
    let workflowRunner = workflowRunnerFactory.Create(domainName = "Product")
    let orderWorkflow = OrderWorkflow()

    let simulatedErrorProvider = SimulatedErrorProvider()
    let invoiceRepository = InvoiceRepository(simulatedErrorProvider)
    let notificationClient = NotificationClient(simulatedErrorProvider)
    let orderRepository = OrderRepository(simulatedErrorProvider)
    let paymentRepository = PaymentRepository(simulatedErrorProvider)
    let warehouseClient = WarehouseClient(simulatedErrorProvider)

    let orderId = OrderId.New()
    let orderToCreate: CreateOrder = { OrderId = orderId; Price = 100m }

    let formatStatus =
        function
        | OrderCreated -> "Created"
        | OrderCancelled -> "Cancelled"
        | OrderPaid _ -> "Paid"
        | OrderInvoiced _ -> "Invoiced"
        | OrderShipped _ -> "Shipped"

    let (|FromTo|) (cmd: TransitionOrder) = formatStatus cmd.Transition.From, formatStatus cmd.Transition.To

    interface IDisposable with
        override _.Dispose() = serviceProvider.Dispose()

    member private _.PrepareInstructions() =
        fun (preparer: IInstructionPreparer<'ins>) ->
            { new IOrderInstructions with
                member _.CreateOrder =
                    preparer // ↩
                        .Command(orderRepository.CreateOrder, "CreateOrder")
                        .Revert(fun cmd _ -> orderRepository.DeleteOrder cmd.OrderId)

                member _.IssueInvoice =
                    preparer
                        .Command(invoiceRepository.IssueInvoice, "IssueInvoice")
                        .Compensate(fun _ res -> invoiceRepository.CompensateInvoice { InvoiceId = res |> Result.force })

                member _.ProcessPayment =
                    preparer
                        .Command(paymentRepository.ProcessPayment, "ProcessPayment")
                        .Compensate(fun _ res -> paymentRepository.RefundPayment { PaymentId = res |> Result.force })

                member _.SendNotification =
                    preparer // ↩
                        .Command(notificationClient.SendNotification, fun cmd -> $"SendNotificationOrder%s{formatStatus cmd.NewStatus}")
                        .NoUndo()

                member _.ShipOrder =
                    preparer // ↩
                        .Command(warehouseClient.ShipOrder, "ShipOrder")
                        .NoUndo()

                member _.TransitionOrder =
                    preparer
                        .Command(orderRepository.TransitionOrder, fun (FromTo(from, to')) -> $"TransitionOrderFrom%s{from}To%s{to'}")
                        .Revert(fun cmd _ -> orderRepository.TransitionOrder(cmd.Revert()))
            }

    member private this.VerifyUndo(simulate, expectedHistory) =
        async {
            do simulatedErrorProvider.Define simulate
            let expectedError = simulate.Error

            let! result, sagaState = workflowRunner.RunInSaga orderWorkflow orderToCreate (this.PrepareInstructions())

            let! orderCreated = orderRepository.GetOrderById orderId

            test
                <@
                    result = Error expectedError
                    && orderCreated = None
                    && sagaState.Status = SagaStatus.Failed(originalError = expectedError, undoErrors = [])
                    && lightHistory sagaState = expectedHistory
                @>
        }

    [<Test>]
    member this.``undo: 1_ no steps given createOrder failed``() =
        this.VerifyUndo(
            simulate = { Error = DataError(DuplicateKey(Id = orderId.ToString(), Type = "Order")); When = OrderAction.CreateOrder },
            expectedHistory = []
        )

    [<Test>]
    member this.``undo: 2_ createOrder given processPayment failed``() =
        this.VerifyUndo(
            simulate = { Error = DataError(DataNotFound(Id = orderId.ToString(), Type = "Order")); When = OrderAction.ProcessPayment },
            expectedHistory = [ "CreateOrder", UndoDone ]
        )

    [<Test>]
    member this.``undo: 3_ createOrder and processPayment given issueInvoice failed``() =
        this.VerifyUndo(
            simulate = { Error = OperationNotAllowed { Operation = "IssueInvoice"; Reason = "Simulated" }; When = OrderAction.IssueInvoice },
            expectedHistory = [
                "SendNotificationOrderPaid", RunDone
                "TransitionOrderFromCreatedToPaid", UndoDone
                "ProcessPayment", UndoDone
                "CreateOrder", UndoDone
            ]
        )

    [<Test>]
    member this.``undo: 4_ createOrder, processPayment, and issueInvoice given shipOrder failed``() =
        this.VerifyUndo(
            simulate = {
                Error = DataError(HttpApiError(HttpApiName "Warehouse", HttpStatus.FromHttpStatusCode HttpStatusCode.ServiceUnavailable))
                When = OrderAction.ShipOrder
            },
            expectedHistory = [
                "SendNotificationOrderInvoiced", RunDone
                "TransitionOrderFromPaidToInvoiced", UndoDone
                "IssueInvoice", UndoDone
                "SendNotificationOrderPaid", RunDone
                "TransitionOrderFromCreatedToPaid", UndoDone
                "ProcessPayment", UndoDone
                "CreateOrder", UndoDone
            ]
        )

// TODO: test cancel order after each step...