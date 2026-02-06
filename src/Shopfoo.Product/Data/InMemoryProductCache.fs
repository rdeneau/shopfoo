namespace Shopfoo.Product.Data

open System.Collections.Concurrent
open FsToolkit.ErrorHandling
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales

type internal InMemoryProductCache() =
    let cache = ConcurrentDictionary<FSID, Product * Prices>()

    member _.TryGetProduct(fsid) = cache.TryGetValue(fsid) |> Option.ofPair |> Option.map fst
    member _.TryGetPrice(fsid) = cache.TryGetValue(fsid) |> Option.ofPair |> Option.map snd

    member _.TryGetAllProducts() =
        match cache.Values |> Seq.toList with
        | [] -> None
        | xs -> Some [ for product, _ in xs -> product ]

    member private _.Set(fsid, ?product: Product, ?prices: Prices) =
        result {
            let! entry =
                match product, prices, cache.TryGetValue(fsid) |> Option.ofPair with
                | Some product, Some prices, _ -> Ok(product, prices)
                | Some product, None, Some(_, existingPrices) -> Ok(product, existingPrices)
                | None, Some prices, Some(existingProduct, _) -> Ok(existingProduct, prices)
                | None, None, Some(existingProduct, existingPrices) -> Ok(existingProduct, existingPrices)
                | _, _, None -> Error(GuardClause { EntityName = nameof FSID; ErrorMessage = "Either product or prices must be provided" })

            cache.AddOrUpdate(fsid, entry, fun _ _ -> entry) |> ignore
            return ()
        }

    member private _.WhenFromFakeStore(sku: SKU, f) =
        let error = GuardClause { EntityName = nameof SKU; ErrorMessage = $"Expected FSID, received %s{sku.Value}" }

        sku.Match( // ↩
            withFSID = f,
            withISBN = (fun _ -> Error error),
            withOLID = (fun _ -> Error error)
        )

    member this.SetProduct(product: Product) = this.WhenFromFakeStore(product.SKU, fun fsid -> this.Set(fsid, product = product))
    member this.SetPrices(prices: Prices) = this.WhenFromFakeStore(prices.SKU, fun fsid -> this.Set(fsid, prices = prices))

    member this.SetAll(entries) =
        cache.Clear()

        entries
        |> List.traverseResultM (fun (fsid, product: Product, prices: Prices) ->
            if product.SKU = prices.SKU then
                this.Set(fsid, product, prices)
            else
                Error(GuardClause { EntityName = nameof SKU; ErrorMessage = "Mismatched SKUs between product and prices" })
        )
        |> Result.map ignore