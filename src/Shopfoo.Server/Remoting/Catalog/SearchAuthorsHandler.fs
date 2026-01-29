namespace Shopfoo.Server.Remoting.Catalog

open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type SearchAuthorsHandler(api: FeatApi) =
    inherit SecureQueryHandler<SearchAuthorsRequest, SearchAuthorsResponse>()

    override _.Handle _ request user =
        async {
            let! result = api.Product.SearchAuthors(request.SearchTerm)
            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok data -> response.Ok { Authors = Set.ofList data.Authors; TotalCount = data.TotalCount }
        }