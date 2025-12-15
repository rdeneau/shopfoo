module Shopfoo.Client.App

open Feliz

let appView () =
    ReactContexts.FullContext.Provider [ View.AppView() ]

ReactDOM
    .createRoot(Browser.Dom.document.getElementById "shopfoo-app") // ↩
    .render (appView ())