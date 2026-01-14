/// <summary>
/// Types and extension methods related to HTTP requests and responses
/// (<c>HttpContent</c>, <c>HttpRequestMessage</c>, <c>HttpResponseMessage</c>)
/// to use in data sources.
/// </summary>
module Shopfoo.Data.Http

open System
open System.IO
open System.IO.Compression
open System.Net.Http
open System.Text
open Shopfoo.Data.Serialization
open Shopfoo.Domain.Types.Errors

type HttpContent with
    /// Reads the content as a string, either regular or GZipped.
    member this.ReadAsGZippableStringAsync() =
        task {
            if this.Headers.ContentEncoding.Contains("gzip") then
                let! stream = this.ReadAsStreamAsync()

                use gzip = new GZipStream(stream, CompressionMode.Decompress)

                use reader = new StreamReader(gzip, Encoding.UTF8)

                return! reader.ReadToEndAsync()
            else
                return! this.ReadAsStringAsync()
        }

type HttpMethod with
    member this.ToVerb() : HttpVerb =
        match this.Method.ToUpperInvariant() with
        | "GET" -> HttpVerb.Get
        | "POST" -> HttpVerb.Post
        | "PUT" -> HttpVerb.Put
        | "PATCH" -> HttpVerb.Patch
        | "DELETE" -> HttpVerb.Delete
        | method -> invalidArg (nameof this.Method) $"HTTP method %s{method} not supported"

type StringHttpRequest = {
    Verb: HttpVerb
    Uri: Uri
    Content: StringContent option
} with

    static member Create(verb, uri, ?content) : StringHttpRequest = {
        Content = content
        Verb = verb
        Uri = uri
    }

    static member OfRequestMessage(rq: HttpRequestMessage) =
        StringHttpRequest.Create( // ↩
            verb = rq.Method.ToVerb(),
            uri = rq.RequestUri,
            ?content = (rq.Content :?> StringContent |> Option.ofObj)
        )

    member this.ReadContentAsync() =
        match this.Content with
        | None -> task { return None }
        | Some content ->
            task {
                let! content = content.ReadAsStringAsync()
                return Some content
            }

type HttpRequestMessage with
    member this.WithHeaders(headers) =
        headers |> List.iter (fun (key, value) -> this.Headers.Add(name = key, value = value))
        this

    static member Get(uri: Uri, ?content) = new HttpRequestMessage(HttpMethod.Get, uri, Content = Option.toObj content)
    static member Post(uri: Uri, ?content) = new HttpRequestMessage(HttpMethod.Post, uri, Content = Option.toObj content)
    static member Put(uri: Uri, ?content) = new HttpRequestMessage(HttpMethod.Put, uri, Content = Option.toObj content)
    static member Patch(uri: Uri, ?content) = new HttpRequestMessage(HttpMethod.Patch, uri, Content = Option.toObj content)
    static member Delete(uri: Uri, ?content) = new HttpRequestMessage(HttpMethod.Delete, uri, Content = Option.toObj content)

type HttpResponseMessage with
    member private this.TryAsync(readContent, ?rq: StringHttpRequest) =
        task {
            if this.IsSuccessStatusCode then
                let! content = readContent this
                return Ok content
            else
                let! content = this.Content.ReadAsStringAsync()

                match rq with
                | Some rq ->
                    let! request = rq.ReadContentAsync()
                    return Error(HttpStatus.FromHttpStatusCode(this.StatusCode, content, ?request = request, verb = rq.Verb, uri = rq.Uri))
                | None -> // ↩
                    return Error(HttpStatus.FromHttpStatusCode(this.StatusCode, content))
        }

    member this.TryReadContentAsStreamAsync() = // ↩
        this.TryAsync(_.Content.ReadAsStreamAsync())

    member this.TryReadContentAsStringAsync(rq: HttpRequestMessage) =
        this.TryAsync(_.Content.ReadAsStringAsync(), StringHttpRequest.OfRequestMessage(rq))

    member this.TryReadContentAsStringAsync(verb, uri, ?request) =
        this.TryAsync(_.Content.ReadAsStringAsync(), StringHttpRequest.Create(verb, uri, ?content = request))

    member this.TryReadGZippableContentAsStringAsync(rq: HttpRequestMessage) =
        this.TryAsync(_.Content.ReadAsGZippableStringAsync(), StringHttpRequest.OfRequestMessage(rq))

    member this.ToResultAsync(rq: StringHttpRequest) = this.TryAsync((fun _ -> task { return () }), rq)
    member this.ToResultAsync(rq: HttpRequestMessage) = this.ToResultAsync(StringHttpRequest.OfRequestMessage(rq))
    member this.ToResultAsync(verb, uri, ?request) = this.ToResultAsync(StringHttpRequest.Create(verb, uri, ?content = request))

type HttpApiSerializer(serializer: ISerializer, httpApiName) =
    member _.Encode(payLoad) = new StringContent(serializer.Serialize payLoad, Encoding.UTF8, serializer.ContentType)
    member _.Serialize(payLoad) = serializer.Serialize payLoad
    member _.TryDeserializeResult<'a>(result) = Serializer.TryDeserializeApiResult(result, httpApiName, serializer.TryDeserialize<'a>)

type HttpApiSerializerFactory(jsonSerializer: IJsonSerializer, xmlSerializer: IXmlSerializer) =
    member _.Json(httpApiName) = HttpApiSerializer(jsonSerializer, httpApiName)
    member _.Xml(httpApiName) = HttpApiSerializer(xmlSerializer, httpApiName)

type Uri with
    static member Relative(path: string) = Uri(path, UriKind.Relative)