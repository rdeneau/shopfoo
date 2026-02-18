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
open Shopfoo.Program.Tests.Light
open Shopfoo.Program.Tests.Mocks
open Swensen.Unquote
open TUnit.Core

type OrderWorkflowSagaShould() =
    let services = ServiceCollection().AddProgramMocks()
    let serviceProvider = services.BuildServiceProvider()
    let workflowRunnerFactory = serviceProvider.GetRequiredService<IWorkflowRunnerFactory>()
    let workflowRunner = workflowRunnerFactory.Create(domainName = "Product")
    let simulatedErrorProvider = SimulatedErrorProvider()
    let invoiceRepository = InvoiceRepository simulatedErrorProvider
    let notificationClient = NotificationClient simulatedErrorProvider
    let orderRepository = OrderRepository simulatedErrorProvider
    let paymentRepository = PaymentRepository simulatedErrorProvider
    let warehouseClient = WarehouseClient simulatedErrorProvider

    let orderId = OrderId.New()
    let cmdCreateOrder: CreateOrder = { OrderId = orderId; Price = 100m }

    let (|FromTo|) (cmd: TransitionOrder) = cmd.Transition.From.Name, cmd.Transition.To.Name

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
                        .Command(notificationClient.SendNotification, fun cmd -> $"SendNotificationOrder%s{cmd.NewStatus.Name}")
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

            let! result, sagaState = workflowRunner.RunInSaga (OrderWorkflow()) cmdCreateOrder (this.PrepareInstructions())

            let! orderCreated = orderRepository.GetOrderById orderId

            test
                <@
                    result = Error expectedError
                    && orderCreated = None
                    && sagaState.Status = SagaStatus.Failed(originalError = expectedError, undoErrors = [])
                    && lightHistory sagaState = expectedHistory
                @>
        }

    member private this.VerifyCancel(cancelAfterStep, expectedStatus, expectedHistory, ?expectedError) =
        async {
            let! result, sagaState = workflowRunner.RunInSaga (OrderWorkflow(cancelAfterStep)) cmdCreateOrder (this.PrepareInstructions())

            let! orderCreated = orderRepository.GetOrderById orderId

            let expectedOrder = {
                Id = orderId
                Price = cmdCreateOrder.Price
                LightStatus = expectedStatus
            }

            let expectedError, expectedStatus =
                match expectedError with
                | None -> WorkflowError(WorkflowCancelled(string cancelAfterStep)), SagaStatus.Cancelled
                | Some err -> err, SagaStatus.Failed(err, undoErrors = [])

            test
                <@
                    result = Error expectedError
                    && orderCreated |> Option.map lightOrder |> Option.contains expectedOrder
                    && sagaState.Status = expectedStatus
                    && lightHistory sagaState = expectedHistory
                @>
        }

    [<Test>]
    member this.``1a: undo no steps given createOrder failed``() =
        this.VerifyUndo(
            simulate = { Error = DataError(DuplicateKey(Id = orderId.ToString(), Type = "Order")); Step = OrderStep.CreateOrder },
            expectedHistory = []
        )

    [<Test>]
    member this.``1b: cancel without undo after createOrder``() =
        this.VerifyCancel(
            cancelAfterStep = OrderStep.CreateOrder,
            expectedStatus = LightOrderCancelled LightOrderCreated,
            expectedHistory = [ // ↩
                "TransitionOrderFromCreatedToCancelled", RunDone
                "CreateOrder", RunDone
            ]
        )

    [<Test>]
    member this.``2a: undo createOrder given processPayment failed``() =
        this.VerifyUndo(
            simulate = { Error = DataError(DataNotFound(Id = orderId.ToString(), Type = "Order")); Step = OrderStep.ProcessPayment },
            expectedHistory = [ "CreateOrder", UndoDone ]
        )

    [<Test>]
    member this.``2b: cancel without undo after processPayment``() =
        this.VerifyCancel(
            cancelAfterStep = OrderStep.ProcessPayment,
            expectedStatus = LightOrderCancelled LightOrderPaid,
            expectedHistory = [ // ↩
                "TransitionOrderFromPaidToCancelled", RunDone
                "SendNotificationOrderPaid", RunDone
                "TransitionOrderFromCreatedToPaid", RunDone
                "ProcessPayment", RunDone
                "CreateOrder", RunDone
            ]
        )

    [<Test>]
    member this.``3a: undo createOrder and processPayment given issueInvoice failed``() =
        this.VerifyUndo(
            simulate = { Error = OperationNotAllowed { Operation = "IssueInvoice"; Reason = "Simulated" }; Step = OrderStep.IssueInvoice },
            expectedHistory = [
                "SendNotificationOrderPaid", RunDone
                "TransitionOrderFromCreatedToPaid", UndoDone
                "ProcessPayment", UndoDone
                "CreateOrder", UndoDone
            ]
        )

    [<Test>]
    member this.``3b: cancel without undo after issueInvoice``() =
        this.VerifyCancel(
            cancelAfterStep = OrderStep.IssueInvoice,
            expectedStatus = LightOrderCancelled LightOrderInvoiced,
            expectedHistory = [ // ↩
                "TransitionOrderFromInvoicedToCancelled", RunDone
                "SendNotificationOrderInvoiced", RunDone
                "TransitionOrderFromPaidToInvoiced", RunDone
                "IssueInvoice", RunDone

                "SendNotificationOrderPaid", RunDone
                "TransitionOrderFromCreatedToPaid", RunDone
                "ProcessPayment", RunDone
                "CreateOrder", RunDone
            ]
        )

    [<Test>]
    member this.``4a: undo createOrder, processPayment, and issueInvoice given shipOrder failed``() =
        this.VerifyUndo(
            simulate = {
                Error = DataError(HttpApiError(HttpApiName "Warehouse", HttpStatus.FromHttpStatusCode HttpStatusCode.ServiceUnavailable))
                Step = OrderStep.ShipOrder
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

    // TODO: test 4b
    [<Test; Skip("TODO RDE")>]
    member this.``4b: fail to cancel and still not undo after shipOrder``() =
        this.VerifyCancel(
            cancelAfterStep = OrderStep.ShipOrder,
            expectedStatus = LightOrderShipped, // Not LightOrderCancelled LightOrderShipped
            expectedError = BusinessError OrderCannotBeCancelledAfterShipping,
            expectedHistory = [ // ↩
                "SendNotificationOrderShipped", RunDone
                "TransitionOrderFromInvoicedToShipped", RunDone
                "ShipOrder", RunDone

                "SendNotificationOrderInvoiced", RunDone
                "TransitionOrderFromPaidToInvoiced", RunDone
                "IssueInvoice", RunDone

                "SendNotificationOrderPaid", RunDone
                "TransitionOrderFromCreatedToPaid", RunDone
                "ProcessPayment", RunDone
                "CreateOrder", RunDone
            ]
        )