module Shopfoo.Client.App

open Feliz

ReactDOM
    .createRoot(Browser.Dom.document.getElementById "shopfoo-app") // ↩
    .render (View.AppView())