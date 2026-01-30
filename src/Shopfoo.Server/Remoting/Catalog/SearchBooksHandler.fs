namespace Shopfoo.Server.Remoting.Catalog

open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type SearchBooksHandler(api: FeatApi) =
    inherit SecureQueryHandler<SearchBooksRequest, SearchBooksResponse>()

    override _.Handle _ request user =
        async {
            let! result = api.Product.SearchBooks(request.SearchTerm)
            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok data -> response.Ok { Books = data.Books; TotalCount = data.TotalCount }
        }