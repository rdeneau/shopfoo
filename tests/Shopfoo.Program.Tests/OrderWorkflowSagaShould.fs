namespace Shopfoo.Product.Tests

open System
open System.Net
open Microsoft.Extensions.DependencyInjection
open Shopfoo.Common
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Tests.OrderContext
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

    let invoiceRepository = InvoiceRepository()
    let notificationClient = NotificationClient()
    let orderRepository = OrderRepository()
    let paymentRepository = PaymentRepository()
    let warehouseClient = WarehouseClient()

    let uniqueString (prefix: string) = prefix + Guid.NewGuid().ToString()[0..7]
    let errorMessage = uniqueString "simulated error #"

    let expectedErrors = {|
        createOrder = DataError(DuplicateKey(Id = errorMessage, Type = "Order"))
        processPayment = DataError(DataNotFound(Id = errorMessage, Type = "Order"))
        issueInvoice = OperationNotAllowed { Operation = "issueInvoice"; Reason = errorMessage }
        shipOrder = HttpApiError(HttpApiName "Warehouse", HttpStatus.FromHttpStatusCode HttpStatusCode.ServiceUnavailable)
    |}

    let undoChangeOrderStatus (cmd: Cmd.ChangeOrderStatus) _res = orderRepository.ChangeOrderStatus(cmd.Revert())
    let undoCreateOrder (cmd: Cmd.CreateOrder) _res = orderRepository.DeleteOrder cmd.OrderId
    let undoInvoice _cmd (res: Result<InvoiceId, Error>) = invoiceRepository.CompensateInvoice { InvoiceId = res |> Result.force }
    let undoPayment _cmd (res: Result<PaymentId, Error>) = paymentRepository.RefundPayment { PaymentId = res |> Result.force }

    interface IDisposable with
        override _.Dispose() = serviceProvider.Dispose()

    member private _.PrepareInstructions(?createOrderError, ?processPaymentError, ?issueInvoiceError, ?notificationError, ?shipOrderError) =
        fun (x: IWorkflowPreparer<'ins>) ->
            { new IOrderInstructions with
                member _.ChangeOrderStatus =
                    x.PrepareInstruction // ↩
                        "ChangeOrderStatus"
                        orderRepository.ChangeOrderStatus
                        _.Command.Revert(undoChangeOrderStatus)

                member _.CreateOrder =
                    x.PrepareInstruction
                        "CreateOrder"
                        (orderRepository.CreateOrder(?simulatedError = createOrderError))
                        _.Command.Revert(undoCreateOrder)

                member _.IssueInvoice =
                    x.PrepareInstruction
                        "IssueInvoice"
                        (invoiceRepository.IssueInvoice(?simulatedError = issueInvoiceError))
                        _.Command.Compensate(undoInvoice)

                member _.ProcessPayment =
                    x.PrepareInstruction
                        "ProcessPayment"
                        (paymentRepository.ProcessPayment(?simulatedError = processPaymentError))
                        _.Command.Compensate(undoPayment)

                member _.SendNotification =
                    x.PrepareInstruction
                        "SendNotification"
                        (notificationClient.SendNotification(?simulatedError = notificationError))
                        _.Command.NoUndo()

                member _.ShipOrder =
                    x.PrepareInstruction // ↩
                        "ShipOrder"
                        (warehouseClient.ShipOrder(?simulatedError = shipOrderError))
                        _.Command.NoUndo()
            }

    [<Test>]
    member this.``undo no steps given the first step—createOrder—failed``() =
        async {
            let orderId = OrderId.New()
            let orderToCreate: Cmd.CreateOrder = { OrderId = orderId; Price = 100m }
            let expectedError = expectedErrors.createOrder

            let! result, sagaState = workflowRunner.RunInSaga orderWorkflow orderToCreate (this.PrepareInstructions(createOrderError = expectedError))

            let! orderCreated = orderRepository.GetOrderById orderId

            test
                <@
                    result = Error expectedError
                    && orderCreated = None
                    && sagaState = { Status = SagaStatus.Failed(originalError = expectedError, undoErrors = []); History = [] }
                @>
        }

    [<Test>]
    member this.``first step—createOrder—undone (reverted) given the second step—processPayment—failed``() =
        async {
            let orderId = OrderId.New()
            let orderToCreate: Cmd.CreateOrder = { OrderId = orderId; Price = 100m }
            let expectedError = expectedErrors.processPayment

            let! result, sagaState =
                workflowRunner.RunInSaga orderWorkflow orderToCreate (this.PrepareInstructions(processPaymentError = expectedError))

            let! orderCreated = orderRepository.GetOrderById orderId

            test
                <@
                    result = Error expectedError
                    && orderCreated = None
                    && sagaState.Status = SagaStatus.Failed(originalError = expectedError, undoErrors = [])
                    && lightHistory sagaState = [ { InstructionName = "CreateOrder"; Status = StepStatus.UndoDone } ]
                @>
        }

// TODO: add additional tests for other failure scenarios, e.g. ProcessPayment fails and CreateOrder is undone successfully, etc.
// TODO: test cancel order after each step...