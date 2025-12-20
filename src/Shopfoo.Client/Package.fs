[<RequireQualifiedAccess>]
module Shopfoo.Client.Package

open Fable.Core

[<ImportMember("../../package.json")>]
let author: string = jsNative

[<ImportMember("../../package.json")>]
let homepage: string = jsNative

[<ImportMember("../../package.json")>]
let repository: {| url: string |} = jsNative

[<ImportMember("../../package.json")>]
let releaseDate: string = jsNative

[<ImportMember("../../package.json")>]
let version: string = jsNative