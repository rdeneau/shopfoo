[<RequireQualifiedAccess>]
module Shopfoo.Product.Model.Stock

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Warehouse

let verifyNoStock (stock: Stock) =
    validation {
        let! _ =
            Guard(nameof Stock) // ↩
                .Satisfies(stock.Quantity = 0, "Stock quantity must be zero to mark as sold out.")
                .ToValidation()

        return ()
    }