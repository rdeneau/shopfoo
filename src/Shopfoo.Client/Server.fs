module Shopfoo.Client.Server

open Fable.Remoting.Client
open Shopfoo.Shared.Remoting

/// A proxy you can use to talk to server directly
let api: RootApi =
    let options =
        Remoting.createApi () // ↩
        |> Remoting.withRouteBuilder Route.builder

    {
        Catalog = Remoting.buildProxy options
        Home = Remoting.buildProxy options
        Prices = Remoting.buildProxy options
    }