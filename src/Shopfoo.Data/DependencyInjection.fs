module Shopfoo.Data.DependencyInjection

open System.Net.Http
open Microsoft.Extensions.DependencyInjection
open Shopfoo.Data.Serialization

type HttpClient with
    member private client.Accept mediaType =
        client.DefaultRequestHeaders.Accept.Add(Headers.MediaTypeWithQualityHeaderValue mediaType)

    member client.AcceptJson() = client.Accept Json.MediaType
    member client.AcceptXml() = client.Accept Xml.MediaType

type IServiceCollection with
    member services.AddHttp() =
        services
            .AddSingleton<IJsonSerializer, Json.Serializer>()
            .AddSingleton<IXmlSerializer, Xml.Serializer>()
            .AddSingleton<Http.HttpApiSerializerFactory>()