module Shopfoo.Client.Components.AppNav

open System
open Feliz
open Feliz.DaisyUI
open Shopfoo.Client
open Shopfoo.Client.Filters
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types
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
                prop.className [
                    match cssClass with
                    | Some css -> css
                    | None -> ()
                ]
                prop.children [
                    if page = currentPage then
                        Html.text text
                    else
                        Html.a [
                            prop.key $"nav-link-%s{page.Key}"
                            prop.text text
                            yield! prop.hrefRouted page
                        ]
                ]
            ]

    member nav.Home = nav.page (Page.Home, "🛍️ Shopfoo")
    member nav.About = nav.page (Page.About, translate _.Home.About)
    member nav.Admin = nav.page (Page.Admin, translate _.Home.Admin)
    member nav.Login = nav.page (Page.Login, translate _.Home.Login)
    member nav.Products = nav.page (Page.ProductIndexDefaults, translate _.Home.Products)
    member nav.Bazaar = nav.page (Page.ProductIndexDefaultsWith _.ToBazaar(), translate _.Home.Bazaar)
    member nav.Books = nav.page (Page.ProductIndexDefaultsWith _.ToBooks(), translate _.Home.Books)
    member nav.Product sku = nav.page (Page.ProductDetail sku, text = sku.Value, cssClass = "font-semibold")

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
                            | Page.ProductIndex { CategoryFilters = None } -> nav.Products

                            | Page.ProductIndex { CategoryFilters = Some(CategoryFilters.Bazaar _) } ->
                                nav.Products
                                nav.Bazaar

                            | Page.ProductIndex { CategoryFilters = Some(CategoryFilters.Books _) } ->
                                nav.Products
                                nav.Books

                            | Page.ProductDetail sku ->
                                nav.Products

                                match sku.Type with
                                | SKUType.FSID _ -> nav.Bazaar
                                | SKUType.ISBN _ -> nav.Books
                                | SKUType.Unknown -> ()

                                nav.Product sku
                        ]
                    ]
                ]
            ]

            yield! children
        ]
    ]