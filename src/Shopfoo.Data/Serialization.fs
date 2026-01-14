module Shopfoo.Data.Serialization

open Shopfoo.Domain.Types.Errors

type internal Serializer =
    static member inline TryDeserialize
        (
            content: 'content, // ↩
            [<InlineIfLambda>] deserialize: 'content -> 'out,
            [<InlineIfLambda>] stringify: 'content -> string
        ) =
        try
            Ok(deserialize content)
        with exn ->
            Error(DeserializationIssue(stringify content, typeof<'content>.FullName, exn))

    static member inline TryDeserializeApiResult(result, httpApiName, [<InlineIfLambda>] tryDeserialize) =
        match result with
        | Ok content -> tryDeserialize content
        | Error httpStatus -> Error(HttpApiError(httpApiName, httpStatus))

[<Interface>]
type ISerializer =
    abstract ContentType: string
    abstract Serialize: source: obj -> string
    abstract Deserialize<'a> : content: string -> 'a

[<Interface>]
type IJsonSerializer =
    inherit ISerializer

[<Interface>]
type IXmlSerializer =
    inherit ISerializer

[<AutoOpen>]
module SerializerExtensions =
    type ISerializer with
        member this.TryDeserialize<'a>(content: string) = // ↩
            Serializer.TryDeserialize(content, this.Deserialize<'a>, stringify = id)