[<RequireQualifiedAccess>]
module internal Shopfoo.Home.Data.Translations

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Translations

let private translationPages = [
    PageCode.Home,
    [
        TagCode "About", "About", "À propos"
        TagCode "AboutDisclaimer",
        "This application is a demo project showcasing the SAFE functional architecture, with domain workflows based on pseudo algebraic effects.",
        "Cette application est un projet démo illustrant l'architecture 'SAFE functional', avec des 'domain workflows' basé sur des pseudos effets algébriques."

        TagCode "Admin", "Admin", "Administration"
        TagCode "AdminDisclaimer",
        "This page is only used to verify the user access. Refresh the page, log in with a non-admin user, and check the redirection to the NotFound page.",
        "Cette page n'a d'autre intérêt que de servir à vérifier les droits d'accès utilisateur. Pour cela, rafraîchir la page, se connecter avec un utilisateur non admin et vérifier la redirection vers la page NotFound."

        TagCode "Login", "Login", "Connexion"
        TagCode "Logout", "Logout", "Se déconnecter"
        TagCode "Page", "Page", "Page"
        TagCode "Product", "Product", "Produit"
        TagCode "Products", "Products", "Produits"

        TagCode "Required", "Required", "Requis"

        TagCode "ChangeLangOk", "Successful switch to English", "Passage au français réussi"
        TagCode "ChangeLangError", "Switching to {0} failed: {1}", "Passage de la langue en {0} en erreur : {1}"
        TagCode "ErrorNotFound", "{0} not found:", "{0} non trouvé :"

        TagCode "Theme.Dark", "Dark", "Sombre (Dark)"
        TagCode "Theme.Light", "Light", "Clair (Light)"
        TagCode "Theme.Business", "Business", "Affaires (Business)"
        TagCode "Theme.Corporate", "Corporate", "Entreprise (Corporate)"
        TagCode "Theme.Garden", "Garden", "Jardin (Garden)"
        TagCode "Theme.Night", "Night", "Nuit (Night)"
        TagCode "Theme.Nord", "Nord", "Nord"
        TagCode "Theme.Dim", "Dim", "Dim"

        TagCode "ThemeGroup.Dark", "Dark themes", "Thèmes sombres"
        TagCode "ThemeGroup.Light", "Light themes", "Thèmes clairs"
    ]

    PageCode.Login,
    [
        TagCode "Access.Edit", "Edit", "Édition"
        TagCode "Access.View", "View", "Affichage"

        TagCode "Feat.Admin", "Admin", "Admin"
        TagCode "Feat.About", "About", "À propos"
        TagCode "Feat.Catalog", "Catalog", "Catalogue"
        TagCode "Feat.Sales", "Sales", "Ventes"
        TagCode "Feat.Warehouse", "Warehouse", "Entrepôt"

        TagCode "SelectPersona", "Select a persona based on access rights", "Choisir un persona en fonction des droits d'accès"
        TagCode "Persona", "Persona", "Persona"
    ]

    PageCode.Product,
    [
        TagCode "Actions", "Actions", "Actions"
        TagCode "CatalogInfo", "Catalog Info", "Info Catalogue"
        TagCode "Description", "Description", "Description"
        TagCode "ImageUrl", "Image Url", "Url de l'image"
        TagCode "Name", "Name", "Nom"

        TagCode "RetailPrice", "Retail price", "Prix de vente recommandé"

        TagCode "PriceAction.Increase", "Increase price", "Augmenter le prix"
        TagCode "PriceAction.Decrease", "Decrease price", "Réduire le prix"
        TagCode "PriceAction.Unavailable", "Unavailable", "Rendre indisponible"

        TagCode "Stock", "Stock", "Stock"
        TagCode "StockAction.InventoryAdjustment", "Inventory adjustment", "Ajuster suite à inventaire"

        TagCode "Save", "Save", "Enregistrer"
        TagCode "SaveOk", "Product {0} saved successfully", "Enregistrement réussi du produit {0}"
        TagCode "SaveError", "Failed to save the product {0}: {1}", "Erreur lors de l'enregistrement du produit {0} : {1}"
    ]
]

let private mapTranslationPages f = [
    for pageCode, translations in translationPages do
        pageCode, translations |> List.map f |> Map.ofList
]

let repository =
    Map [
        Lang.English, mapTranslationPages (fun (tagCode, en, _) -> tagCode, en)
        Lang.French, mapTranslationPages (fun (tagCode, _, fr) -> tagCode, fr)
    ]