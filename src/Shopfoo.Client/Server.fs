module Shopfoo.Client.Server

open Fable.Remoting.Client
open Shopfoo.Shared.Remoting

let private options = Remoting.createApi () |> Remoting.withRouteBuilder Route.builder

/// A proxy you can use to talk to server directly
let api: RootApi = {
    Catalog = Remoting.buildProxy options
    Home = Remoting.buildProxy options
    Prices = Remoting.buildProxy options
}