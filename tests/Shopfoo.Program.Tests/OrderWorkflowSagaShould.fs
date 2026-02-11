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

    let simulatedErrorProvider = SimulatedErrorProvider()
    let invoiceRepository = InvoiceRepository(simulatedErrorProvider)
    let notificationClient = NotificationClient(simulatedErrorProvider)
    let orderRepository = OrderRepository(simulatedErrorProvider)
    let paymentRepository = PaymentRepository(simulatedErrorProvider)
    let warehouseClient = WarehouseClient(simulatedErrorProvider)

    let orderId = OrderId.New()
    let orderToCreate: Cmd.CreateOrder = { OrderId = orderId; Price = 100m }

    let undoCreateOrder (cmd: Cmd.CreateOrder) _res = orderRepository.DeleteOrder cmd.OrderId
    let undoTransitionOrder (cmd: Cmd.TransitionOrder) _res = orderRepository.TransitionOrder(cmd.Revert())
    let undoInvoice _cmd (res: Result<InvoiceId, Error>) = invoiceRepository.CompensateInvoice { InvoiceId = res |> Result.force }
    let undoPayment _cmd (res: Result<PaymentId, Error>) = paymentRepository.RefundPayment { PaymentId = res |> Result.force }

    interface IDisposable with
        override _.Dispose() = serviceProvider.Dispose()

    member private _.PrepareInstructions() =
        fun (x: IInstructionPreparer<'ins>) ->
            { new IOrderInstructions with
                member _.CreateOrder = x.Prepare orderRepository.CreateOrder _.Command("CreateOrder").Revert(undoCreateOrder) // Include the Do in the build step ?
                member _.IssueInvoice = x.Prepare invoiceRepository.IssueInvoice _.Command("IssueInvoice").Compensate(undoInvoice)
                member _.ProcessPayment = x.Prepare paymentRepository.ProcessPayment _.Command("ProcessPayment").Compensate(undoPayment)
                member _.SendNotification = x.Prepare notificationClient.SendNotification _.Command("SendNotification").NoUndo()
                member _.ShipOrder = x.Prepare warehouseClient.ShipOrder _.Command("ShipOrder").NoUndo()
                member _.TransitionOrder = x.Prepare orderRepository.TransitionOrder _.Command("TransitionOrder").Revert(undoTransitionOrder)
            }

    member private this.VerifyUndo(expectedError, errorAt, expectedHistory) =
        async {
            do simulatedErrorProvider.Define expectedError errorAt

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
    member this.``1_ undo no steps given createOrder failed``() =
        this.VerifyUndo(
            expectedError = DataError(DuplicateKey(Id = orderId.ToString(), Type = "Order")),
            errorAt = OrderAction.CreateOrder,
            expectedHistory = []
        )

    [<Test>]
    member this.``2_ undo createOrder given processPayment failed``() =
        this.VerifyUndo(
            expectedError = DataError(DataNotFound(Id = orderId.ToString(), Type = "Order")),
            errorAt = OrderAction.ProcessPayment,
            expectedHistory = [ "CreateOrder", StepStatus.UndoDone ]
        )

    [<Test>]
    member this.``3_ undo createOrder and processPayment given issueInvoice failed``() =
        this.VerifyUndo(
            expectedError = OperationNotAllowed { Operation = "IssueInvoice"; Reason = "Simulated" },
            errorAt = OrderAction.IssueInvoice,
            expectedHistory = [
                "SendNotification", StepStatus.RunDone
                "TransitionOrder", StepStatus.UndoDone
                "ProcessPayment", StepStatus.UndoDone
                "CreateOrder", StepStatus.UndoDone
            ]
        )

    [<Test>]
    member this.``4_ undo createOrder, processPayment, and issueInvoice given shipOrder failed``() =
        this.VerifyUndo(
            expectedError = DataError(HttpApiError(HttpApiName "Warehouse", HttpStatus.FromHttpStatusCode HttpStatusCode.ServiceUnavailable)),
            errorAt = OrderAction.ShipOrder,
            expectedHistory = [
                "SendNotification", StepStatus.RunDone
                "TransitionOrder", StepStatus.UndoDone
                "IssueInvoice", StepStatus.UndoDone
                "SendNotification", StepStatus.RunDone
                "TransitionOrder", StepStatus.UndoDone
                "ProcessPayment", StepStatus.UndoDone
                "CreateOrder", StepStatus.UndoDone
            ]
        )

// TODO: test cancel order after each step...