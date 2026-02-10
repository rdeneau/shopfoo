namespace Shopfoo.Product.Tests

open System
open Shopfoo.Domain.Types.Errors
open Shopfoo.Program
open Shopfoo.Program.Dependencies
open Shopfoo.Program.Runner
open Swensen.Unquote
open TUnit.Core

type OrderId = OrderId of Guid

type Order = {
    Id: OrderId
    Price: decimal
    Created: bool
    PaymentProcessed: bool
    InvoiceIssued: bool
    Shipped: bool
}

[<Interface>]
type IOrderInstructions =
    inherit IProgramInstructions
    abstract member CreateOrder: (Order -> Async<Result<unit, Error>>)
    abstract member ProcessPayment: (OrderId -> Async<Result<unit, Error>>)
    abstract member IssueInvoice: (OrderId -> Async<Result<unit, Error>>)
    abstract member ShipOrder: (OrderId -> Async<Result<unit, Error>>)

module Program =
    type private DefineProgram = DefineProgram<IOrderInstructions>

    let createOrder order = DefineProgram.instruction _.CreateOrder(order)
    let processPayment orderId = DefineProgram.instruction _.ProcessPayment(orderId)
    let issueInvoice orderId = DefineProgram.instruction _.IssueInvoice(orderId)
    let shipOrder orderId = DefineProgram.instruction _.ShipOrder(orderId)

type OrderWorkflow() =
    interface IProgramWorkflow<IOrderInstructions, Order, unit> with
        override _.Run order =
            program {
                do! Program.createOrder order
                do! Program.processPayment order.Id
                do! Program.issueInvoice order.Id
                do! Program.shipOrder order.Id
                return Ok()
            }

type OrderRepository() =
    let mutable orders: Map<OrderId, Order> = Map.empty

    let getOrderById orderId = async { return orders |> Map.tryFind orderId }

    member _.InitOrder price : Order = {
        Id = OrderId(Guid.NewGuid())
        Price = price
        Created = false
        PaymentProcessed = false
        InvoiceIssued = false
        Shipped = false
    }

    member _.CreateOrder(?error) = fun orderToCreate ->
        async {
            match error with
            | None ->
                let! order = getOrderById orderToCreate.Id

                match order with
                | None ->
                    orders <- orders.Add(orderToCreate.Id, { orderToCreate with Created = true })
                    return Ok()
                | Some _ -> return bug (exn "Order already exists")

            | Some err -> return bug (exn err)
        }

    member _.ProcessPayment(?error) = fun orderId ->
        async {
            match error with
            | None ->
                let! order = getOrderById orderId

                match order with
                | Some o when o.Created && not o.PaymentProcessed ->
                    orders <- orders.Add(orderId, { o with PaymentProcessed = true })
                    return Ok()
                | Some _ -> return bug (exn "Invalid order state for processing payment")
                | None -> return bug (exn "Order not found")

            | Some err -> return bug (exn err)
        }

    member _.IssueInvoice(?error) = fun orderId ->
        async {
            match error with
            | None ->
                let! order = getOrderById orderId

                match order with
                | Some o when o.PaymentProcessed && not o.InvoiceIssued ->
                    orders <- orders.Add(orderId, { o with InvoiceIssued = true })
                    return Ok()
                | Some _ -> return bug (exn "Invalid order state for issuing invoice")
                | None -> return bug (exn "Order not found")

            | Some err -> return bug (exn err)
        }

    member _.ShipOrder(?error) = fun orderId ->
        async {
            match error with
            | None ->
                let! order = getOrderById orderId

                match order with
                | Some o when o.InvoiceIssued ->
                    orders <- orders.Add(orderId, { o with Shipped = true })
                    return Ok()
                | Some _ -> return bug (exn "Invalid order state for shipping order")
                | None -> return bug (exn "Order not found")

            | Some err -> return bug (exn err)
        }

type SagaUndoTests() =
    let repo = OrderRepository()
    let workflow = OrderWorkflow()
    let workflowRunnerFactory: IWorkflowRunnerFactory = todo
    let workflowRunner = workflowRunnerFactory.Create(domainName = "Product")

    [<Test>]
    member _.``no steps undone given the first step—createOrder—failed``() =
        async {
            let prepareInstructions (x: IWorkflowPreparer<'ins>) =
                { new IOrderInstructions with
                    member _.CreateOrder = x.PrepareCommand("CreateOrder", repo.CreateOrder(error = "CreateOrderFailure"))
                    member _.ProcessPayment = x.PrepareCommand("ProcessPayment", repo.ProcessPayment())
                    member _.IssueInvoice = x.PrepareCommand("IssueInvoice", repo.IssueInvoice())
                    member _.ShipOrder = x.PrepareCommand("ShipOrder", repo.ShipOrder())
                }

            let order = repo.InitOrder(price = 100m)

            let! result, sagaState =
                workflowRunner.RunInSaga workflow order prepareInstructions

            result =! Ok()
            // TODO: check sagaState
        }