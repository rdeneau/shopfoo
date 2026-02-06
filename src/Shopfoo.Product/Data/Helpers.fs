/// Helpers for constructing fake sales and stock events, used in both pseudo-production and tests.
module Shopfoo.Product.Data.Helpers

open System
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse

let daysAgo (days: int) = DateOnly.FromDateTime(DateTime.Now.AddDays(-days))

type ISBN with
    member isbn.Events stockEvents : StockEvent list = [ for stockEvent in stockEvents -> { stockEvent with SKU = isbn.AsSKU } ]
    member isbn.Sales(sales: Sale seq) = [ for sale in sales -> { sale with SKU = isbn.AsSKU } ]

type Units =
    static member private For eventType date quantity : StockEvent = {
        SKU = SKUUnknown.SKUUnknown.AsSKU
        Date = date
        Quantity = quantity
        Type = eventType
    }

    static member Purchased price = Units.For(EventType.ProductSupplyReceived price)
    static member Remaining = Units.For EventType.StockAdjusted

let unitsSold price date quantity : Sale = {
    SKU = SKUUnknown.SKUUnknown.AsSKU
    Date = date
    Price = price
    Quantity = quantity
}