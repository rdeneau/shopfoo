module Shopfoo.Client.Pages.Product.Details.CatalogInfo

open System
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Glutinum.Iconify
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Icon
open type Shopfoo.Client.Components.MultiSelect
open Shopfoo.Client.Pages.Product
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Client.Search
open Shopfoo.Client.UI
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Shopfoo.Shared.Translations

type private Model = {
    Product: Remote<Product>
    BooksData: Remote<GetBooksDataResponse>
    SaveDate: Remote<DateTime>
    SearchedAuthors: Remote<SearchAuthorsResponse>
}

type private Msg =
    | ProductFetched of ApiResult<GetProductResponse * Translations>
    | BooksFetched of ApiResult<GetBooksDataResponse>
    | ProductChanged of Product
    | SaveProduct of Product * ApiCall<unit>
    | AddProduct of Product * ApiCall<unit>
    | SearchAuthors of searchTerm: string
    | AuthorsSearched of ApiResult<SearchAuthorsResponse>
    | ClearSearchedAuthors

[<RequireQualifiedAccess>]
module private Cmd =
    let loadBooksData (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Catalog.GetBooksData request
            Error = Error >> BooksFetched
            Success = Ok >> BooksFetched
        }

    let loadProduct (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Catalog.GetProduct request
            Error = Error >> ProductFetched
            Success = Ok >> ProductFetched
        }

    let saveProduct (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Catalog.SaveProduct request
            Error = fun err -> SaveProduct(request.Body, Done(Error err))
            Success = fun () -> SaveProduct(request.Body, Done(Ok()))
        }

    let addProduct (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Catalog.AddProduct request
            Error = fun err -> AddProduct(request.Body, Done(Error err))
            Success = fun () -> AddProduct(request.Body, Done(Ok()))
        }

    let searchAuthors (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Catalog.SearchAuthors request
            Error = Error >> AuthorsSearched
            Success = Ok >> AuthorsSearched
        }

let private init (fullContext: FullContext) sku =
    {
        Product = Remote.Loading
        BooksData = Remote.Empty
        SaveDate = Remote.Empty
        SearchedAuthors = Remote.Empty
    },
    Cmd.loadProduct (fullContext.PrepareQueryWithTranslations sku)

let private update fillTranslations onSaveProduct (fullContext: FullContext) (msg: Msg) (model: Model) =
    match msg with
    | BooksFetched(Ok booksData) -> { model with BooksData = Remote.Loaded booksData }, Cmd.none
    | BooksFetched(Error apiError) -> { model with BooksData = Remote.LoadError apiError }, Cmd.none

    | ProductFetched(Error apiError) ->
        { model with Product = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations apiError.Translations)

    | ProductFetched(Ok(response, translations)) ->
        let booksData, cmd =
            match response.Product, fullContext.User.AccessTo Feat.Catalog with
            | Some { Category = Category.Books _ }, Some Access.Edit -> Remote.Loading, Cmd.loadBooksData (fullContext.PrepareRequest())
            | _ -> Remote.Loaded { Authors = Set.empty; Tags = Set.empty }, Cmd.none

        { model with Product = response.Product |> Remote.ofOption; BooksData = booksData }, // ↩
        Cmd.batch [ Cmd.ofEffect (fun _ -> fillTranslations translations); cmd ]

    | ProductChanged product -> // ↩
        { model with Product = Remote.Loaded product }, Cmd.none

    | SaveProduct(product, Start) ->
        { model with SaveDate = Remote.Loading }, // ↩
        Cmd.saveProduct (fullContext.PrepareRequest product)

    | SaveProduct(product, Done result) ->
        let saveDate = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult

        { model with SaveDate = saveDate }, // ↩
        Cmd.ofEffect (fun _ -> onSaveProduct (product, result |> Result.tryGetError))

    | AddProduct(product, Start) ->
        { model with SaveDate = Remote.Loading }, // ↩
        Cmd.addProduct (fullContext.PrepareRequest product)

    | AddProduct(product, Done result) ->
        let saveDate = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult
        let error = result |> Result.tryGetError

        { model with SaveDate = saveDate },
        Cmd.batch [
            match product.Category, product.SKU.Type with
            | Category.Books book, SKUType.OLID _ when error.IsNone ->
                let product = { product with SKU = book.ISBN.AsSKU }
                Cmd.navigatePage (Page.ProductDetail book.ISBN.AsSKU)
                Cmd.ofEffect (fun _ -> onSaveProduct (product, error))
            | _ -> // ↩
                Cmd.ofEffect (fun _ -> onSaveProduct (product, error))
        ]

    | SearchAuthors searchTerm ->
        { model with SearchedAuthors = Remote.Loading }, // ↩
        Cmd.searchAuthors (fullContext.PrepareRequest { SearchTerm = searchTerm })

    | AuthorsSearched(Ok response) -> { model with SearchedAuthors = Remote.Loaded response }, Cmd.none
    | AuthorsSearched(Error apiError) -> { model with SearchedAuthors = Remote.LoadError apiError }, Cmd.none
    | ClearSearchedAuthors -> { model with SearchedAuthors = Remote.Empty }, Cmd.none

type private Fieldset(catalogAccess, product: Product, translations: AppTranslations, dispatch) =
    let bookChanged book = // ↩
        ProductChanged { product with Category = Category.Books book }

    let propsOrReadonly (props: IReactProperty seq) = [
        match catalogAccess with
        | Some Access.Edit -> yield! props
        | _ ->
            prop.readOnly true
            prop.className "bg-base-300"
    ]

    let propOnChangeOrReadonly (handler: string -> unit) = propsOrReadonly [ prop.onChange handler ]
    let propOnCheckedChangeOrReadonly handler = propsOrReadonly [ prop.onCheckedChange handler ]

    member _.bazaarCategory(bazaarProduct: BazaarProduct) =
        let categoryRadio (category: BazaarCategory) (iconifyIcon: IconifyIcon) (text: string) =
            let key = String.toKebab $"%s{category.ToString()}"

            Daisy.fieldset [
                prop.key $"fieldset-%s{key}"
                prop.className "w-auto"
                prop.children [
                    Daisy.fieldsetLabel [
                        prop.key $"label-%s{key}"
                        prop.className "flex flex-row items-center gap-2 cursor-pointer text-sm"
                        prop.children [
                            Daisy.radio [
                                prop.key $"radio-%s{key}"
                                prop.name "category"
                                prop.value key
                                prop.isChecked (bazaarProduct.Category = category)
                                yield!
                                    propOnCheckedChangeOrReadonly (fun isChecked ->
                                        if isChecked then
                                            let productCategory = Category.Bazaar { bazaarProduct with Category = category }

                                            dispatch (ProductChanged { product with Category = productCategory })
                                    )
                            ]
                            icon iconifyIcon
                            Html.text text
                        ]
                    ]
                ]
            ]

        let categoryInfo (category: BazaarCategory) (iconifyIcon: IconifyIcon) = {|
            category = category
            icon = iconifyIcon
            text = translations.Product.StoreCategoryOf category
        |}

        let categoryInfos = [
            categoryInfo BazaarCategory.Jewelry fa6Solid.gem
            categoryInfo BazaarCategory.Clothing fa6Solid.shirt
            categoryInfo BazaarCategory.Electronics fa6Solid.tv
        ]

        Daisy.fieldset [
            prop.key "category-fieldset"
            prop.className "mb-2"
            prop.children [
                Daisy.fieldsetLabel [ prop.key "category-label"; prop.children [ Html.text translations.Product.Category ] ]
                Html.div [
                    prop.key "category-container"
                    prop.className "input focus-within:outline-none focus-within:border-transparent w-full gap-4 mb-4"
                    prop.className [
                        "input w-full gap-4 mb-4"
                        // Remove input focus styles to avoid clashing with radio buttons
                        "focus-within:border-[color-mix(in_oklab,var(--color-base-content)_20%,#0000)]"
                        "focus-within:outline-none"
                        "focus-within:shadow-none"
                    ]
                    prop.children [
                        for x in categoryInfos |> List.sortBy _.text do
                            categoryRadio x.category x.icon x.text
                    ]
                ]
            ]
        ]

    member _.bookAuthors
        (
            book: Book,
            booksData: GetBooksDataResponse,
            remoteSearchedAuthors: Remote<SearchAuthorsResponse>,
            authorSearchInfo: SearchInfo,
            setAuthorSearchInfo: SearchInfo -> unit
        ) =
        let toggleAuthor (isChecked, author) = // ↩
            dispatch (bookChanged { book with Authors = book.Authors.Toggle(author, isChecked) })

        let searchedAuthors =
            match remoteSearchedAuthors with
            | Remote.Loaded response -> response.Authors
            | _ -> Set.empty

        let searchMoreButtonProps =
            if String.IsNullOrWhiteSpace authorSearchInfo.SearchTerm then
                SearchButtonProps.None
            else
                match remoteSearchedAuthors with
                | Remote.Empty ->
                    SearchButtonProps.Active(
                        icon = fa6Solid.magnifyingGlass,
                        tooltip = translations.Product.SearchAuthors,
                        onSearch =
                            fun () ->
                                dispatch (SearchAuthors authorSearchInfo.SearchTerm)
                                AfterSearch(shouldClearTerm = false)
                    )
                | Remote.Loading -> SearchButtonProps.Searching
                | Remote.LoadError apiError -> SearchButtonProps.SearchComplete(SearchCompletionStatus.Error apiError.ErrorMessage)
                | Remote.Loaded { Authors = Set.Empty } ->
                    SearchButtonProps.SearchComplete(SearchCompletionStatus.Info translations.Product.NoAuthorsFound)
                | Remote.Loaded data when data.TotalCount > data.Authors.Count ->
                    SearchButtonProps.SearchComplete(
                        SearchCompletionStatus.Info(translations.Product.AuthorSearchLimit(limit = data.Authors.Count, totalFound = data.TotalCount))
                    )
                | Remote.Loaded _ -> SearchButtonProps.SearchComplete SearchCompletionStatus.Success

        let allAuthors =
            Set.unionMany [
                book.Authors
                booksData.Authors
                searchedAuthors
            ]

        let authorItems = [
            for author in allAuthors do
                let cover =
                    Cover.BookAuthor(
                        key = $"author-{author.OLID.Value}",
                        author = author,
                        container = CoverContainer.LinkToAuthorCard,
                        sizeCssClass = "w-[40px] min-w-[40px] h-[40px] min-h-[40px]",
                        translations = translations
                    )

                let symbol = if searchedAuthors.Contains author then "✨" else ""

                {
                    Text = author.Name + symbol
                    Value = author
                    Image = Some cover
                    Selected = book.Authors.Contains author
                    Filterable = not (searchedAuthors.Contains author)
                    SearchTarget = SearchTarget.BookAuthor author
                }
        ]

        Daisy.fieldset [
            prop.key "authors-fieldset"
            prop.className "mb-2"
            prop.children [
                Daisy.fieldsetLabel [ prop.key "authors-label"; prop.children [ Html.text translations.Product.Authors ] ]
                MultiSelect(
                    key = "authors-select",
                    items = authorItems,
                    onSearchTermChange =
                        (fun searchInfo ->
                            setAuthorSearchInfo searchInfo
                            dispatch ClearSearchedAuthors
                        ),
                    onSelect = toggleAuthor,
                    readonly = (catalogAccess <> Some Edit),
                    translations = translations,
                    searchMoreButtonProps = searchMoreButtonProps
                )
            ]
        ]

    member _.bookSubtitle(book: Book) =
        Daisy.fieldset [
            prop.key "subtitle-fieldset"
            prop.className "mb-2"
            prop.children [
                let props = Product.Guard.BookSubtitle.props (book.Subtitle, translations)

                Daisy.fieldsetLabel [
                    prop.key "subtitle-label"
                    prop.children [
                        Html.text translations.Product.Subtitle
                        Html.small [ prop.key "subtitle-required"; yield! props.textRequired ]
                        Html.small [ prop.key "subtitle-spacer"; prop.className "flex-1" ]
                        Html.span [ prop.key "subtitle-char-count"; yield! props.textCharCount ]
                    ]
                ]

                Daisy.validator.input [
                    prop.key "subtitle-input"
                    prop.className "w-full"
                    prop.placeholder translations.Product.Subtitle
                    props.value
                    yield! props.validation
                    yield! propOnChangeOrReadonly (fun subtitle -> dispatch (bookChanged { book with Subtitle = subtitle }))
                ]
            ]
        ]

    member _.bookTags(book: Book, booksData: GetBooksDataResponse, tagSearchInfo: SearchInfo, setTagSearchInfo: SearchInfo -> unit) =
        let toggleTag (isChecked, tag) = // ↩
            dispatch (bookChanged { book with Tags = book.Tags.Toggle(tag, isChecked) })

        let tagItems = [
            for tag in Set.union book.Tags booksData.Tags do
                {
                    Text = tag
                    Value = tag
                    Image = None
                    Selected = book.Tags.Contains tag
                    Filterable = true
                    SearchTarget = SearchTarget.BookTag tag
                }
        ]

        let searchMoreButtonProps =
            let tag = tagSearchInfo.SearchTerm.Trim()

            if tag.Length > 0 && not (booksData.Tags.Contains tag) then
                SearchButtonProps.Active(
                    icon = fa6Solid.plus,
                    tooltip = translations.Product.AddTag,
                    onSearch =
                        fun () ->
                            let productCategory = Category.Books { book with Tags = book.Tags.Add tag }
                            dispatch (ProductChanged { product with Category = productCategory })
                            AfterSearch(shouldClearTerm = true)
                )
            else
                SearchButtonProps.None

        Daisy.fieldset [
            prop.key "tags-fieldset"
            prop.className "mb-2"
            prop.children [
                Daisy.fieldsetLabel [ prop.key "tags-label"; prop.text translations.Product.Tags ]
                MultiSelect(
                    key = "tags-select",
                    items = tagItems,
                    onSelect = toggleTag,
                    onSearchTermChange = setTagSearchInfo,
                    readonly = (catalogAccess <> Some Edit),
                    translations = translations,
                    searchMoreButtonProps = searchMoreButtonProps
                )
            ]
        ]

    member _.description() =
        let props = Product.Guard.Description.props (product.Description, translations)

        Daisy.fieldset [
            prop.key "description-fieldset"
            prop.className "mb-2"
            prop.children [
                Daisy.fieldsetLabel [
                    prop.key "description-label"
                    prop.children [
                        Html.text translations.Product.Description
                        Html.small [ prop.key "description-required"; yield! props.textRequired ]
                        Html.small [ prop.key "description-spacer"; prop.className "flex-1" ]
                        Html.span [ prop.key "description-char-count"; yield! props.textCharCount ]
                    ]
                ]

                Daisy.textarea [
                    prop.key "description-textarea"
                    prop.className "validator h-21 w-full"
                    prop.placeholder translations.Product.Description
                    props.value
                    yield! props.validation
                    yield! propOnChangeOrReadonly (fun description -> dispatch (ProductChanged { product with Description = description }))
                ]
            ]
        ]

    member _.imageUrl() =
        Daisy.fieldset [
            prop.key "image-fieldset"
            prop.className "mb-2"
            prop.children [
                let props = Product.Guard.ImageUrl.props (product.ImageUrl.Url, translations, invalid = product.ImageUrl.Broken)

                Daisy.fieldsetLabel [
                    prop.key "image-label"
                    prop.children [
                        Html.text translations.Product.ImageUrl
                        Html.small [ prop.key "image-required"; yield! props.textRequired ]
                        Html.small [ prop.key "image-spacer"; prop.className "flex-1" ]
                        Html.span [ prop.key "image-char-count"; yield! props.textCharCount ]
                    ]
                ]

                Daisy.validator.input [
                    prop.key "image-input"
                    prop.className "w-full"
                    prop.placeholder translations.Product.ImageUrl
                    props.value
                    yield! props.validation
                    yield! propOnChangeOrReadonly (fun url -> dispatch (ProductChanged { product with ImageUrl = ImageUrl.Valid url }))
                ]
            ]
        ]

    member _.name() =
        Daisy.fieldset [
            prop.key "name-fieldset"
            prop.className "mb-2"
            prop.children [
                let props = Product.Guard.Name.props (product.Title, translations)

                Daisy.fieldsetLabel [
                    prop.key "name-label"
                    prop.children [
                        Html.text translations.Product.Name
                        Html.small [ prop.key "name-required"; yield! props.textRequired ]
                        Html.small [ prop.key "name-spacer"; prop.className "flex-1" ]
                        Html.span [ prop.key "name-char-count"; yield! props.textCharCount ]
                    ]
                ]

                Daisy.validator.input [
                    prop.key "name-input"
                    prop.className "w-full"
                    prop.placeholder translations.Product.Name
                    props.value
                    yield! props.validation
                    yield! propOnChangeOrReadonly (fun name -> dispatch (ProductChanged { product with Title = name }))
                ]
            ]
        ]

[<ReactComponent>]
let CatalogInfoForm key (fullContext: FullContext) (productModel: ProductModel) fillTranslations onSaveProduct =
    let sku = productModel.SKU
    let translations = fullContext.Translations
    let catalogAccess = fullContext.User.AccessTo Feat.Catalog

    let model, dispatch =
        React.useElmish (init fullContext sku, update fillTranslations onSaveProduct fullContext, [| box sku |])

    let authorSearchInfo, setAuthorSearchInfo = React.useState SearchInfo.empty
    let tagSearchInfo, setTagSearchInfo = React.useState SearchInfo.empty

    let buildImagePreview (product: Product) =
        Html.div [
            prop.key "image-preview-column"
            prop.className "h-[230px] w-[180px] relative overflow-hidden flex items-center justify-center rounded-box"
            prop.children [
                // 1. Conditional Rendering: Image vs Broken Link Fallback
                if product.ImageUrl.Broken then
                    Html.div [
                        prop.key "image-fallback"
                        prop.className [
                            "flex flex-col items-center justify-center w-full h-full"
                            "bg-gray-100 border border-base-300 text-error text-3xl"
                        ]
                        prop.children (icon fa6Solid.linkSlash)
                    ]
                else
                    Html.img [
                        prop.key "image-preview"
                        prop.src product.ImageUrl.Url
                        prop.className [
                            "w-full h-full object-contain transition-all"
                            if productModel.SoldOut then
                                "grayscale opacity-40"
                        ]
                        prop.onError (fun (_: Browser.Types.Event) -> dispatch (ProductChanged { product with Product.ImageUrl.Broken = true }))
                    ]

                // 2. Sold Out Ribbon (Optimized geometry for visual centering)
                if productModel.SoldOut then
                    Html.div [
                        prop.key "image-sold-out-overlay"
                        prop.className "absolute inset-0 overflow-hidden pointer-events-none"
                        prop.children [
                            Html.div [
                                prop.key "image-sold-out-text"
                                prop.className [
                                    "absolute top-8 -right-12 w-48 rotate-45"
                                    "bg-red-600 text-white text-[10px] font-bold py-1 shadow-md uppercase tracking-wider"
                                    "flex items-center justify-center text-center"
                                ]
                                prop.text translations.Product.SoldOut
                            ]
                        ]
                    ]
            ]
        ]

    React.fragment [
        match model.Product with
        | Remote.Empty ->
            Daisy.alert [
                alert.error
                prop.key "product-not-found"
                prop.children [
                    Html.span [
                        prop.key "pnf-icon"
                        prop.text "⛓️‍💥"
                        prop.className "text-lg mr-1"
                    ]
                    Html.span [
                        prop.key "pnf-content"
                        prop.children [
                            Html.span [ prop.key "pnf-text"; prop.text (translations.Home.ErrorNotFound translations.Home.Product) ]
                            Html.code [ prop.key "pnf-sku"; prop.text $" %s{sku.Value} " ]
                        ]
                    ]
                ]
            ]

        | Remote.Loading -> Daisy.skeleton [ prop.className "h-64 w-full"; prop.key "products-skeleton" ]
        | Remote.LoadError apiError -> Alert.apiError "product-load-error" apiError fullContext.User

        | Remote.Loaded product ->
            // -- New Product Alert ----

            let isNewProduct =
                match product.Category, sku.Type with
                | Category.Books _, SKUType.OLID _ -> true
                | _ -> false

            if isNewProduct then
                Daisy.alert [
                    alert.info
                    prop.key "catalog-info-alert"
                    prop.className "mb-3"
                    prop.children [
                        Html.div [
                            prop.key "catalog-info-alert-content"
                            prop.className "flex-col items-start"
                            prop.children [
                                Html.h3 [
                                    prop.key "catalog-info-alert-title"
                                    prop.className "font-bold flex items-center gap-2 mb-1"
                                    prop.children [ icon fa6Solid.circleInfo; Html.text $"%s{translations.Product.NewProductTitle} ✨" ]
                                ]
                                Html.div [ prop.key "catalog-info-alert-disclaimer"; prop.text translations.Product.NewProductDisclaimer ]
                            ]
                        ]
                    ]
                ]

            // -- Product Fieldsets ----

            let fieldset = Fieldset(catalogAccess, product, translations, dispatch)

            Daisy.fieldset [
                prop.key $"%s{key}-fieldset"
                prop.className "bg-base-200 border border-base-300 rounded-box p-4"
                prop.children [
                    Html.legend [
                        prop.key "product-details-legend"
                        prop.className "text-sm"
                        prop.text $"🗂️ %s{translations.Product.CatalogInfo}"
                    ]

                    Html.div [
                        prop.key "image-grid"
                        prop.className "grid grid-cols-[1fr_max-content] gap-4 items-center"
                        prop.children [
                            Html.div [
                                prop.key "image-input-column"
                                prop.className "flex flex-col justify-between h-full"
                                prop.children [
                                    fieldset.name ()

                                    match product.Category with
                                    | Category.Books book -> fieldset.bookSubtitle book
                                    | Category.Bazaar bazaarProduct -> fieldset.bazaarCategory bazaarProduct

                                    match product.Category with
                                    | Category.Bazaar _ -> ()
                                    | Category.Books book ->
                                        match model.BooksData with
                                        | Remote.Empty -> ()
                                        | Remote.LoadError apiError -> Alert.apiError "authors-load-error" apiError fullContext.User
                                        | Remote.Loading -> Daisy.skeleton [ prop.className "h-12 w-full"; prop.key "authors-skeleton" ]
                                        | Remote.Loaded booksData ->
                                            fieldset.bookAuthors (book, booksData, model.SearchedAuthors, authorSearchInfo, setAuthorSearchInfo)

                                    fieldset.imageUrl ()
                                ]
                            ]

                            buildImagePreview product
                        ]
                    ]

                    fieldset.description ()

                    match product.Category with
                    | Category.Bazaar _ -> ()
                    | Category.Books book ->
                        match model.BooksData with
                        | Remote.Empty -> ()
                        | Remote.LoadError apiError -> Alert.apiError "tags-load-error" apiError fullContext.User
                        | Remote.Loading -> Daisy.skeleton [ prop.className "h-12 w-full"; prop.key "tags-skeleton" ]
                        | Remote.Loaded booksData -> fieldset.bookTags (book, booksData, tagSearchInfo, setTagSearchInfo)

                    match catalogAccess with
                    | None
                    | Some View -> ()
                    | Some Edit ->
                        let productSku = $"%s{translations.Home.Product} %s{sku.Value}"

                        // Add or Save button
                        if isNewProduct then
                            Buttons.SaveButton(
                                key = "add-product",
                                label = translations.Home.Add,
                                tooltipOk = translations.Home.SavedOk productSku,
                                tooltipError = (fun err -> translations.Home.SavedError(productSku, err.ErrorMessage)),
                                tooltipProps = [ tooltip.right ],
                                saveDate = model.SaveDate,
                                disabled = false,
                                onClick = (fun () -> dispatch (AddProduct(product, Start)))
                            )
                        else
                            Buttons.SaveButton(
                                key = "save-product",
                                label = translations.Home.Save,
                                tooltipOk = translations.Home.SavedOk productSku,
                                tooltipError = (fun err -> translations.Home.SavedError(productSku, err.ErrorMessage)),
                                tooltipProps = [ tooltip.right ],
                                saveDate = model.SaveDate,
                                disabled = false,
                                onClick = (fun () -> dispatch (SaveProduct(product, Start)))
                            )
                ]
            ]
    ]