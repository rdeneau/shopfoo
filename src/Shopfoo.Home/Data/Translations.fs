[<RequireQualifiedAccess>]
module internal Shopfoo.Home.Data.Translations

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Translations

let private translationPages = [
    PageCode.Home,
    [
        TagCode "About", "About", "À propos"
        TagCode "AboutDisclaimer",
        "This application is a demo project showcasing the ❝ Safe Clean Architecture ❞.",
        "Cette application est un projet démo illustrant la ❝ Safe Clean Architecture ❞."

        TagCode "Admin", "Admin", "Administration"
        TagCode "AdminDisclaimer",
        "This page is only used to verify the user access. Refresh the page, log in with a non-admin user, and check the redirection to the NotFound page.",
        "Cette page n'a d'autre intérêt que de servir à vérifier les droits d'accès utilisateur. Pour cela, rafraîchir la page, se connecter avec un utilisateur non admin et vérifier la redirection vers la page NotFound."

        TagCode "Bazaar", "Bazaar", "Bazar"
        TagCode "Books", "Books", "Livres"
        TagCode "Login", "Login", "Connexion"
        TagCode "Logout", "Logout", "Se déconnecter"
        TagCode "Page", "Page", "Page"
        TagCode "Product", "Product", "Produit"
        TagCode "Products", "Products", "Produits"

        TagCode "Cancel", "Cancel", "Annuler"
        TagCode "Close", "Close", "Fermer"
        TagCode "Confirmation", "Confirmation", "Confirmation"
        TagCode "Completed", "Completed", "Réussi"
        TagCode "Error", "Error: {1}", "Erreur : {1}"
        TagCode "Save", "Save", "Enregistrer"
        TagCode "SaveOk", "{0} saved successfully", "{0} enregistré avec succès"
        TagCode "SaveError", "{0} not saved: {1}", "{0} non enregistré : {1}"
        TagCode "Search", "Search", "Rechercher"
        TagCode "Warning", "Warning", "Attention"

        TagCode "Colon", ":", " :"
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
        TagCode "Authors", "Authors", "Auteurs"
        TagCode "CatalogInfo", "Catalog Info", "Info Catalogue"
        TagCode "Category", "Category", "Catégorie"
        TagCode "Description", "Description", "Description"
        TagCode "HighlightMatches", "Highlight matches", "Surligner les résultats"
        TagCode "MatchCase", "Match case", "Sensible à la casse"
        TagCode "ImageUrl", "Image Url", "Url de l'image"
        TagCode "Name", "Name", "Nom"
        TagCode "SoldOut", "Sold-out", "Épuisé"
        TagCode "Tags", "Tags", "Libellés"

        TagCode "Discount", "Discount", "Réduction"
        TagCode "Increase", "Increase", "Augmentation"
        TagCode "Decrease", "Decrease", "Diminution"
        TagCode "Define", "Define", "Définition"
        TagCode "Price", "Price", "Prix"
        TagCode "CurrentPrice", "Current price", "Prix actuel"
        TagCode "NewPrice", "New price", "nouveau prix"
        TagCode "ListPrice", "List price", "Prix catalogue"
        TagCode "RetailPrice", "Retail price", "Prix de vente"

        TagCode "PriceAction.Define", "Define", "Définir"
        TagCode "PriceAction.Remove", "Remove", "Supprimer"
        TagCode "PriceAction.Increase", "Increase price", "Augmenter le prix"
        TagCode "PriceAction.Decrease", "Decrease price", "Diminuer le prix"

        TagCode "PriceAction.RemoveListPriceDialog.Confirm", "Yes, remove the list price", "Oui, supprimer le prix catalogue"
        TagCode "PriceAction.RemoveListPriceDialog.Question",
        "Are you sure you want to remove the list price?",
        "Êtes-vous sûr de vouloir supprimer le prix catalogue ?"

        TagCode "PriceAction.MarkAsSoldOut", "Mark as sold out", "Marquer comme épuisé"
        TagCode "PriceAction.WarnMarkAsSoldOutForbidden",
        "'Mark as sold out' is possible only when the stock is 0.",
        "'Marquer comme épuisé' n'est possible que lorsque le stock est à 0."
        TagCode "PriceAction.MarkAsSoldOutDialog.Confirm", "Yes, mark as sold out", "Oui, marquer comme épuisé"
        TagCode "PriceAction.MarkAsSoldOutDialog.Question",
        "Are you sure you want to mark this product as sold out?",
        "Êtes-vous sûr de vouloir marquer ce produit comme épuisé ?"

        TagCode "StoreCategory.Clothing", "Clothing", "Vêtements"
        TagCode "StoreCategory.Electronics", "Electronics", "Électronique"
        TagCode "StoreCategory.Jewelry", "Jewelry", "Bijoux"

        TagCode "Stock", "Stock", "Stock"
        TagCode "StockAfterInventory", "Stock adjusted after inventory", "Stock réel après inventaire"
        TagCode "StockBeforeInventory", "Stock before inventory", "Stock avant inventaire"
        TagCode "StockAction.AdjustStockAfterInventory", "Inventory adjustment", "Ajuster le stock suite à inventaire"
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