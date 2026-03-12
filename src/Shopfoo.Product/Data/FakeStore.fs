/// Fake Store API
/// https://fakestoreapi.com/
module Shopfoo.Product.Data.FakeStore

open System
open System.Net.Http
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging
open Shopfoo.Data.Http
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales

[<AutoOpen>]
module Dto =
    type ProductId = int

    [<CLIMutable>]
    type ProductDto = {
        Id: ProductId
        Title: string
        Price: decimal
        Description: string
        Category: string
        Image: string
    }

/// Snapshot of the products in Fake Store API at the time of development, used on production because the server can not access the API 🤷
module internal Fakes =
    let mutable private id = 0

    let private nextId () =
        id <- id + 1
        id

    let private productDto category title price description image = {
        Id = nextId ()
        Title = title
        Price = price
        Description = description
        Category = category
        Image = "https://fakestoreapi.com/img/" + image
    }

    let private electronics = productDto "electronics"
    let private jewelery = productDto "jewelery"
    let private mensClothing = productDto "men's clothing"
    let private womensClothing = productDto "women's clothing"

    let all = [
        mensClothing
            "Fjallraven - Foldsack No. 1 Backpack, Fits 15 Laptops"
            109.95M
            "Your perfect pack for everyday use and walks in the forest. Stash your laptop (up to 15 inches) in the padded sleeve, your everyday"
            "81fPKd-2AYL._AC_SL1500_t.png"
        mensClothing
            "Mens Casual Premium Slim Fit T-Shirts"
            22.3M
            "Slim-fitting style, contrast raglan long sleeve, three-button henley placket, light weight & soft fabric for breathable and comfortable wearing..."
            "71-3HjGNDUL._AC_SY879._SX._UX._SY._UY_t.png"
        mensClothing
            "Mens Cotton Jacket"
            55.99M
            "great outerwear jackets for Spring/Autumn/Winter, suitable for many occasions, such as working, hiking, camping, mountain/rock climbing, cycling, traveling or other outdoors..."
            "71li-ujtlUL._AC_UX679_t.png"
        mensClothing
            "Mens Casual Slim Fit"
            15.99M
            "The color could be slightly different between on the screen and in practice..."
            "71YXzeOuslL._AC_UY879_t.png"

        jewelery
            "John Hardy Women's Legends Naga Gold & Silver Dragon Station Chain Bracelet"
            695M
            "From our Legends Collection, the Naga was inspired by the mythical water dragon that protects the ocean's pearl..."
            "71pWzhdJNwL._AC_UL640_QL65_ML3_t.png"
        jewelery
            "Solid Gold Petite Micropave "
            168M
            "Satisfaction Guaranteed. Return or exchange any order within 30 days.Designed and sold by Hafeez Center in the United States..."
            "61sbMiUnoGL._AC_UL640_QL65_ML3_t.png"
        jewelery
            "White Gold Plated Princess"
            9.99M
            "Classic Created Wedding Engagement Solitaire Diamond Promise Ring for Her. Gifts to spoil your love more for Engagement, Wedding, Anniversary, Valentine's Day..."
            "71YAIFU48IL._AC_UL640_QL65_ML3_t.png"
        jewelery
            "Pierced Owl Rose Gold Plated Stainless Steel Double"
            10.99M
            "Rose Gold Plated Double Flared Tunnel Plug Earrings. Made of 316L Stainless Steel"
            "51UDEzMJVpL._AC_UL640_QL65_ML3_t.png"

        electronics
            "WD 2TB Elements Portable External Hard Drive - USB 3.0 "
            64M
            "USB 3.0 and USB 2.0 Compatibility Fast data transfers Improve PC Performance High Capacity; Compatibility Formatted NTFS for Windows 10, ..."
            "61IBBVJvSDL._AC_SY879_t.png"
        electronics
            "SanDisk SSD PLUS 1TB Internal SSD - SATA III 6 Gb/s"
            109M
            "Easy upgrade for faster boot up, shutdown, application load and response (As compared to 5400 RPM SATA 2.5” hard drive; Based on published..."
            "61U7T1koQqL._AC_SX679_t.png"
        electronics
            "Silicon Power 256GB SSD 3D NAND A55 SLC Cache Performance Boost SATA III 2.5"
            109M
            "3D NAND flash are applied to deliver high transfer speeds Remarkable transfer speeds that enable faster bootup and improved overall system performance..."
            "71kWymZ+c+L._AC_SX679_t.png"
        electronics
            "WD 4TB Gaming Drive Works with Playstation 4 Portable External Hard Drive"
            114M
            "Expand your PS4 gaming experience, Play anywhere Fast and easy, setup Sleek design with high capacity, 3-year manufacturer's limited warranty"
            "61mtL65D4cL._AC_SX679_t.png"
        electronics
            "Acer SB220Q bi 21.5 inches Full HD (1920 x 1080) IPS Ultra-Thin"
            599M
            "21. 5 inches Full HD (1920 x 1080) widescreen IPS display And Radeon free Sync technology. No compatibility for VESA Mount Refresh Rate: 75Hz..."
            "81QpkIctqPL._AC_SX679_t.png"
        electronics
            "Samsung 49-Inch CHG90 144Hz Curved Gaming Monitor (LC49HG90DMNXZA) – Super Ultrawide Screen QLED "
            999.99M
            "49 INCH SUPER ULTRAWIDE 32:9 CURVED GAMING MONITOR with dual 27 inch screen side by side QUANTUM DOT (QLED) TECHNOLOGY, HDR support and ..."
            "81Zt42ioCgL._AC_SX679_t.png"

        womensClothing
            "BIYLACLESEN Women's 3-in-1 Snowboard Jacket Winter Coats"
            56.99M
            "Note:The Jackets is US standard size, Please choose size as your usual wear Material: 100% Polyester; Detachable Liner Fabric: Warm Fleece..."
            "51Y5NI-I5jL._AC_UX679_t.png"
        womensClothing
            "Lock and Love Women's Removable Hooded Faux Leather Moto Biker Jacket"
            29.95m
            "100% POLYURETHANE(shell) 100% POLYESTER(lining) 75% POLYESTER 25% COTTON (SWEATER), Faux leather material for style and comfort ..."
            "81XH0e8fefL._AC_UY879_t.png"
        womensClothing
            "Rain Jacket Women Windbreaker Striped Climbing Raincoats"
            39.99m
            "Lightweight perfet for trip or casual wear---Long sleeve with hooded, adjustable drawstring waist design. Button and zipper front closure..."
            "71HblAHs5xL._AC_UY879_-2t.png"
        womensClothing
            "MBJ Women's Solid Short Sleeve Boat Neck V "
            9.85m
            "95% RAYON 5% SPANDEX, Made in USA or Imported, Do Not Bleach, Lightweight fabric with great stretch for comfort, Ribbed on sleeves and..."
            "71z3kpMAYsL._AC_UY879_t.png"
        womensClothing
            "Opna Women's Short Sleeve Moisture"
            7.95m
            "100% Polyester, Machine wash, 100% cationic polyester interlock, Machine Wash & Pre Shrunk for a Great Fit, Lightweight, roomy and..."
            "51eg55uWmdL._AC_UX679_t.png"
        womensClothing
            "DANVOUY Womens T Shirt Casual Cotton Short"
            12.99m
            "95%Cotton,5%Spandex, Features: Casual, Short Sleeve, Letter Print,V-Neck,Fashion Tees, The fabric is soft and has some stretch., ..."
            "61pHAEJ4NML._AC_UX679_t.png"
    ]

[<Interface>]
type IFakeStoreClient =
    abstract member GetProductsAsync: unit -> Task<Result<ProductDto list, DataRelatedError>>

[<RequireQualifiedAccess>]
module private Mappers =
    module DtoToModel =
        let mapProduct storeProduct (product: ProductDto) : Product = {
            Category = Category.Bazaar storeProduct
            SKU = storeProduct.FSID.AsSKU
            Title = product.Title
            Description = product.Description
            ImageUrl = ImageUrl.Valid product.Image
        }

        let mapStoreCategory (category: string) : BazaarCategory option =
            match category.ToLowerInvariant() with
            | "men's clothing"
            | "women's clothing" -> Some BazaarCategory.Clothing
            | "electronics" -> Some BazaarCategory.Electronics
            | "jewelery" -> Some BazaarCategory.Jewelry
            | _ -> None

[<AutoOpen>]
module private Extensions =
    type SKU with
        member this.BindFSID f =
            match this.Type with
            | SKUType.FSID fsid -> f fsid |> Option.map (fun res -> fsid, res)
            | _ -> None

type internal FakeStorePipeline(fakeStoreClient: IFakeStoreClient, logger: ILogger<FakeStorePipeline>) =
    let cache = InMemoryProductCache()

    member _.ResetCache() = cache.Clear()

    member _.GetProducts() : Async<Result<Product list, Error>> =
        asyncResult {
            match cache.TryGetAllProducts() with
            | Some cachedProducts -> // ↩
                return cachedProducts

            | None ->
                let! productsDtos =
                    async {
                        let! res = fakeStoreClient.GetProductsAsync() |> Async.AwaitTask

                        match res with
                        | Ok dtos -> return dtos
                        | Error err ->
                            logger.LogError("FakeStore API error: {Error}", $"%A{err}")
                            return Fakes.all
                    }

                let entries = [
                    for productDto in productsDtos do
                        match Mappers.DtoToModel.mapStoreCategory productDto.Category with
                        | None -> ()
                        | Some storeCategory ->
                            let currency =
                                match storeCategory with
                                | BazaarCategory.Clothing -> EUR
                                | BazaarCategory.Electronics -> USD
                                | BazaarCategory.Jewelry -> EUR

                            let storeProduct = { FSID = FSID productDto.Id; Category = storeCategory }
                            let product = Mappers.DtoToModel.mapProduct storeProduct productDto
                            let prices = Prices.Create(product.SKU, currency, retailPrice = productDto.Price)

                            storeProduct.FSID, product, prices
                ]

                do! cache.SetAll entries

                return entries |> List.map (fun (_, product, _) -> product)
        }

    member _.GetPrice(fsid: FSID) : Async<Prices option> =
        async {
            do! Fake.latencyInMilliseconds 100
            return cache.TryGetPrices(fsid)
        }

    member _.GetProduct(fsid: FSID) : Async<Product option> =
        async {
            do! Fake.latencyInMilliseconds 150
            return cache.TryGetProduct(fsid)
        }

    member _.SavePrices(prices: Prices) : Async<Result<PreviousValue<Prices>, Error>> =
        asyncResult {
            do! Fake.latencyInMilliseconds 280

            match prices.SKU.BindFSID cache.TryGetPrices with
            | Some(fsid, existingPrices) ->
                do! cache.SetPrices(fsid, prices)
                return PreviousValue existingPrices
            | None -> return! Error(DataError(DataNotFound(Id = prices.SKU.Value, Type = nameof Prices)))
        }

    member _.SaveProduct(product: Product) : Async<Result<PreviousValue<Product>, Error>> =
        asyncResult {
            do! Fake.latencyInMilliseconds 400

            match product.SKU.BindFSID cache.TryGetProduct with
            | Some(fsid, existingProduct) ->
                do! cache.SetProduct(fsid, product)
                return PreviousValue existingProduct
            | None -> return! Error(DataError(DataNotFound(Id = product.SKU.Value, Type = nameof Product)))
        }

type internal FakeStoreClient(httpClient: HttpClient, serializerFactory: HttpApiSerializerFactory) =
    let serializer = serializerFactory.Json(HttpApiName.FakeStore)

    member _.GetProductsAsync() =
        task {
            use request = HttpRequestMessage.Get(Uri.Relative "products")
            use! response = httpClient.SendAsync(request)
            let! content = response.TryReadContentAsStringAsync(request)
            return serializer.TryDeserializeResult<ProductDto list>(content)
        }

    interface IFakeStoreClient with
        member this.GetProductsAsync() = this.GetProductsAsync()