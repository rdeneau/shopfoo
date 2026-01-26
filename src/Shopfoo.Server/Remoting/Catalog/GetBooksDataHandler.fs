namespace Shopfoo.Server.Remoting.Catalog

open Shopfoo.Domain.Types.Catalog
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetBooksDataHandler(api: FeatApi) =
    inherit SecureQueryHandler<unit, GetBooksDataResponse>()

    override _.Handle _ () user =
        async {
            let! products = api.Product.GetProducts Provider.OpenLibrary

            let books =
                products
                |> Seq.choose (fun p ->
                    match p.Category with
                    | Category.Books book -> Some book
                    | _ -> None
                )

            let booksData =
                ({ Authors = Set.empty; Tags = Set.empty }, books)
                ||> Seq.fold (fun data book ->
                    let authors = Set.union data.Authors book.Authors
                    let tags = Set.union data.Tags book.Tags
                    { Authors = authors; Tags = tags }
                )

            let response = ResponseBuilder.plain user
            return response.Ok booksData
        }