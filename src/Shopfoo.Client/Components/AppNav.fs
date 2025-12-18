module Shopfoo.Client.Components.AppNav

open System
open Feliz
open Feliz.DaisyUI
open Shopfoo.Client.Routing
open Shopfoo.Shared.Translations

type private Nav(currentPage, translations: AppTranslations) =
    let translate =
        if translations.IsEmpty then
            fun _ -> String.Empty
        else
            fun f -> f translations

    member private nav.page(page: Page, text: string, ?cssClass: string) =
        if String.IsNullOrWhiteSpace(text) then
            Html.none
        else
            Html.li [
                prop.key $"nav-%s{page.Key}"
                prop.text text
                prop.className [
                    if page <> currentPage then
                        "cursor-pointer"

                    match cssClass with
                    | Some css -> css
                    | None -> ()
                ]
                if page <> currentPage then
                    prop.onClick (fun _ -> Router.navigatePage page)
            ]

    member nav.Home = nav.page (Page.Home, "🛍️ Shopfoo")
    member nav.About = nav.page (Page.About, translate _.Home.About)
    member nav.Admin = nav.page (Page.Admin, translate _.Home.Admin)
    member nav.Login = nav.page (Page.Login, translate _.Home.Login)
    member nav.Products = nav.page (Page.ProductIndex, translate _.Home.Products)

    member nav.Product(sku) =
        nav.page (Page.ProductDetail sku, text = sku, cssClass = "font-semibold")

[<ReactComponent>]
let AppNavBar key currentPage pageDisplayedInline translations children =
    let nav = Nav(currentPage, translations)

    Daisy.navbar [
        prop.key $"%s{key}-navbar"
        prop.className "bg-base-200 shadow-sm"
        prop.children [
            Daisy.breadcrumbs [
                prop.className "flex-1"
                prop.key "nav-breadcrumbs"
                prop.children [
                    Html.ul [
                        prop.key "breadcrumbs-ul"
                        prop.children [
                            nav.Home
                            match pageDisplayedInline with
                            | Page.Home
                            | Page.NotFound _ -> ()
                            | Page.About -> nav.About
                            | Page.Admin -> nav.Admin
                            | Page.Login -> nav.Login
                            | Page.ProductIndex -> nav.Products
                            | Page.ProductDetail sku ->
                                nav.Products
                                nav.Product(sku)
                        ]
                    ]
                ]
            ]

            yield! children
        ]
    ]