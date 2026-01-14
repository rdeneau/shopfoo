[<RequireQualifiedAccess>]
module internal Shopfoo.Data.Json

open System.Text.Json
open System.Text.Json.Serialization
open Shopfoo.Data.Serialization

[<Literal>]
let MediaType = "application/json"

type Serializer() =
    let options =
        JsonSerializerOptions(
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        )

    do options.Converters.Add(JsonFSharpConverter())

    interface IJsonSerializer with
        member val ContentType = MediaType
        member _.Serialize(source: obj) = JsonSerializer.Serialize(source, options)
        member _.Deserialize<'a>(content: string) = JsonSerializer.Deserialize<'a>(content, options)