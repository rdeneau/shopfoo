## [1.4.1](https://github.com/rdeneau/shopfoo/compare/v1.4.0...v1.4.1) (2026-03-12)

### Bug Fixes

* 🐛 FakeStore not accessible from Azure ([f8cf980](https://github.com/rdeneau/shopfoo/commit/f8cf98095f13baab64c25f7e75b030acc0cbeaeb))

## [1.4.0](https://github.com/rdeneau/shopfoo/compare/v1.3.0...v1.4.0) (2026-03-12)

### Features

* 💄 improve about page disclaimer ([a842c07](https://github.com/rdeneau/shopfoo/commit/a842c0794208c6a8856f8d70c61b60190373ae71))
* 🖼️ add favicon.png ([e242f21](https://github.com/rdeneau/shopfoo/commit/e242f21f4c33ba56ef47374de02ae5a23a62405e))
* **Actions:** 🖼️ place "Last sale" before "Last purchase" ([1b8b44e](https://github.com/rdeneau/shopfoo/commit/1b8b44ef286cf89a461443791d71d52538cef811))

### Bug Fixes

* ⛓️‍💥 error 404 on Azure ([333776f](https://github.com/rdeneau/shopfoo/commit/333776fadfd12c3cd01dc823494179569cbda722))
* 🐛 "Highlight matches" toggle should apply to bazaar category, book author/tag filters ([0375ba4](https://github.com/rdeneau/shopfoo/commit/0375ba4833434dbb31d976c96d12d764f25285cc))
* 🐛 apply front-end search on OpenLib book search results to display the exact count (N found, P displayed) ([30f7373](https://github.com/rdeneau/shopfoo/commit/30f7373900760cea8a8223a8a97d2c08ac0597cd))
* 🐛 disable "Search Books on Open Library" for Bazaar products ([60d9079](https://github.com/rdeneau/shopfoo/commit/60d9079475daddf6e1ec0b63ce385df2eb348fa8))
* 🐛 reset search term and sort when switching providers ([648c9b7](https://github.com/rdeneau/shopfoo/commit/648c9b70ada3481ccf55f3fe825ddff5a1cadf49))
* 🧪 failing Filters test (to be confirmed) ([cae1219](https://github.com/rdeneau/shopfoo/commit/cae1219641e79eee97caf67e28e5e2d7a58a9aff))
* **ManagePrice:** 🐛 properly prevent price decrease in case of Increase, and vice versa ([a318857](https://github.com/rdeneau/shopfoo/commit/a318857b65301d464a9c3284eb5c126029da56af))

## [1.3.0](https://github.com/rdeneau/shopfoo/compare/v1.2.0...v1.3.0) (2026-03-12)

### Features

*  ✨ add Shopfoo.Data project ([20f98d3](https://github.com/rdeneau/shopfoo/commit/20f98d39812095f5522fcbaeaf853ab89396d41c))
* ✨ add book from the Index page after a "search more" on Open Library ([b636e1e](https://github.com/rdeneau/shopfoo/commit/b636e1e9a01d66b0165d81adb607f39196f298c5))
* ✨ add Error.Errors case and Result.zip ([a110d55](https://github.com/rdeneau/shopfoo/commit/a110d556b4ede1bc93b43c5970dd6c4edf821eb3))
* ✨ add Receive Purchased Products drawer ([8d40910](https://github.com/rdeneau/shopfoo/commit/8d409108540e38dc85cd75edb1aafb928994e6c0))
* ✨ Add ReceiveSupply types and API contract ([4772702](https://github.com/rdeneau/shopfoo/commit/4772702bb56e69ed9de918025d34c5a0b1bba2e9))
* ✨ add ReceiveSupply workflow with validation ([41079e6](https://github.com/rdeneau/shopfoo/commit/41079e643341adc4cf6d3686c13d672d263b4248))
* ✨ display purchase prices with margin ([b5de1db](https://github.com/rdeneau/shopfoo/commit/b5de1db640fff4d1e0b953e26ee2ede077b9d053))
* ✨ display sales stats; input sales ([4b6da71](https://github.com/rdeneau/shopfoo/commit/4b6da71e74b046be8d12c7f5d1c989716051acf6))
* ✨ edit book authors (w/o creation) ([82c3fb1](https://github.com/rdeneau/shopfoo/commit/82c3fb1cae0f49960fea8efdf5574e6e5d4c2f2a))
* ✨ edit book subtitle ([3b34445](https://github.com/rdeneau/shopfoo/commit/3b34445b676a460214e487f5c91cb5eef740fdb2))
* ✨ edit book tags (w/o creation) ([2c7281d](https://github.com/rdeneau/shopfoo/commit/2c7281d8d0582b35919d25f24aad7297f21fce86))
* 👔 add book with empty prices and valid SKU-ISBN ([960dc52](https://github.com/rdeneau/shopfoo/commit/960dc52fd7280c758351cf9db22e09811b096de1))
* 👔 detect and report DuplicateKey during AddPrices ([8109594](https://github.com/rdeneau/shopfoo/commit/8109594eb627296d402eea1da31041c676a5fc2f))
* 👔 display provider cards in product index page when no provider is selected ([71499fc](https://github.com/rdeneau/shopfoo/commit/71499fc61461c74c3250b0fdbb49b4a3563facb2))
* 👔 filter books by tags, add SKU & BookTags columns, extend search ([b74a324](https://github.com/rdeneau/shopfoo/commit/b74a3241f6c11d74cf4ba9e700610361d376f89d))
* 👔 support custom LogLevel in WorkLogger ([c66aab3](https://github.com/rdeneau/shopfoo/commit/c66aab320d7cf1283f3d81e0ef74c2e0e681becb))
* 👔 use retail price (if any) as the default prices for sales to input ([7bfa062](https://github.com/rdeneau/shopfoo/commit/7bfa062ca026b9d4d51fb61ef46712ca4b95c315))
* 👔 wire ReceiveSupply server handler ([d4cbd17](https://github.com/rdeneau/shopfoo/commit/d4cbd17b60cde3eb2fefdaf427362d1d6e5137a1))
* 🖼️ add Checkbox and MultiSelect components ([779955e](https://github.com/rdeneau/shopfoo/commit/779955e52a0ce50cf6c400ec52a0a1b4b69b8791))
* 🖼️ add count indicators in the filter tab ([0628b7f](https://github.com/rdeneau/shopfoo/commit/0628b7f397aed7ef900388610b68d24a535f87d0))
* 🖼️ display last purchase price in an ActionsDropdown component ([bb9fd42](https://github.com/rdeneau/shopfoo/commit/bb9fd42ceba6c981d453e6b874ff09be71d726be))
* 🖼️ edit bazaar category ([16b346d](https://github.com/rdeneau/shopfoo/commit/16b346d1added3e8613e3b3f28ade3ed38b32d8d))
* 🖼️ finalize Admin page: add translations, improve alerts ([7ea6252](https://github.com/rdeneau/shopfoo/commit/7ea62521341f08bea57719a95110230a6caf0afe))
* 🖼️ improve "not found" error display relative to type name ([31a301f](https://github.com/rdeneau/shopfoo/commit/31a301f917d0ba213d3b5d1d84ca797c87771c1c))
* 🖼️ improve bazaar category (location, styles) ([d265086](https://github.com/rdeneau/shopfoo/commit/d265086f16fb8feaadb8963bef714484ed746123))
* 🖼️ improve display relative to product category: books or bazaar ([977de9d](https://github.com/rdeneau/shopfoo/commit/977de9d912540940eadf6d6a197f74970c2fb097))
* 🖼️ show active provider tab ([a9ae587](https://github.com/rdeneau/shopfoo/commit/a9ae587df89aab531c3fee0ff5e377ed9157405f))
* 🖼️ sort provider cards by text (lang sensitive) ([6da4e76](https://github.com/rdeneau/shopfoo/commit/6da4e76351982ebfe2c82d2a990bc182d21ad68c))
* 🚸 Add toast notification on receive supply save ([902e66a](https://github.com/rdeneau/shopfoo/commit/902e66a2642206417d4f86e8ac1c130da2379142))
* **AddProduct:** 🧪 indicate products already there with the new DuplicateKey DataError ([b027071](https://github.com/rdeneau/shopfoo/commit/b027071ba699705883ff9366c1f36334e1b9da75))
* **Admin:** ✨ reset all caches + refactor Persona DU ([cd872f5](https://github.com/rdeneau/shopfoo/commit/cd872f5daa630f26a211250931f1f484d80203af))
* **CatalogInfo:** ✨ search book authors on Open Library in the MultiSelect dropdown ([276436f](https://github.com/rdeneau/shopfoo/commit/276436faeaf709140877396581270f230e6ea9bf))
* **CatalogInfo:** 🖼️ display book author cover in the MultiSelect dropdown content ([6a0e00a](https://github.com/rdeneau/shopfoo/commit/6a0e00afaf6ea7e64525ff8e7a52a9575758dbd3))
* **Effects:** 👔 improve Interpreter ([12acd52](https://github.com/rdeneau/shopfoo/commit/12acd5259a4dbe5382a047583e8b3918beee627d))
* **Filters:** ✨ search options: match case, highlight matches ([4af5c20](https://github.com/rdeneau/shopfoo/commit/4af5c20dbf5d696268fd550585d56ee77a34a975))
* **FullContext:** ✨ add UnitTestSession to ease unit tests (mock api, ignore cmd) ([999dbc8](https://github.com/rdeneau/shopfoo/commit/999dbc84bfed4d7fa46bbab65077394aa28d444a))
* **MultiSelect:** ✨ add "search more button" = "Add tag" for Books Tags ([43b95ee](https://github.com/rdeneau/shopfoo/commit/43b95eec9acda5593d14cbdaaf1da1368c483593))
* **MultiSelect:** ✨ add "search more button" = "Add tag" for Books Tags ([32019d3](https://github.com/rdeneau/shopfoo/commit/32019d372c4bacb7c4c2ae7764cba06a67101d05))
* **MultiSelect:** 🖼️ improve the search box, esp. moved to the dropdown content ([80953bd](https://github.com/rdeneau/shopfoo/commit/80953bdbe2a7e6d7307ad6c862393003bd7c743a))
* **Product:** 👔 support saga (instructions undo) ([fa7394a](https://github.com/rdeneau/shopfoo/commit/fa7394a0fdb709c370e7d3d084c846206dae98d6))
* **Program:** ✨ run workflows in a saga, undoing previously executed commands ([1aa2765](https://github.com/rdeneau/shopfoo/commit/1aa2765fff016145460dcf8ef2236528c8a585c6))
* **Saga:** ✨ add undo criteria ([959b032](https://github.com/rdeneau/shopfoo/commit/959b032e25bf0d75a0c8bbd58de5111fba3106e3))
* **Saga:** ✨ cancel workflow ([afff5bc](https://github.com/rdeneau/shopfoo/commit/afff5bc09d5f3db689c756747e936dbb55830a62))
* **SaveButton:** ✨ print error message as warning in the JS console ([c36a9b6](https://github.com/rdeneau/shopfoo/commit/c36a9b6bb31cdfb6aea51b1f3bc530b87c3ad47c))
* **SearchBox:** 🖼️ have "Search or add" placeholder when search more feat is activated ([789d986](https://github.com/rdeneau/shopfoo/commit/789d986a464afc19bf9dd75269a8d78403c3ea92))
* **Toast:** 🖼️ add Dismiss prop (Auto or Manual) ([82e9bec](https://github.com/rdeneau/shopfoo/commit/82e9bec8363d3a3c1bcb57bd687bf7f3fdd7e853))

### Bug Fixes

* 🐛 adjust IndexTable ([ed440e1](https://github.com/rdeneau/shopfoo/commit/ed440e1cdc204b8ef1ff901a16de59270d97ec1d))
* 🐛 define SKU as a record to overcome JSON serialization issue on the Fable.Remoting side ([63a0b76](https://github.com/rdeneau/shopfoo/commit/63a0b766cd234450868336102744222f40f8462f))
* 🐛 encode Token with Persona, fix missing claims check ([03868df](https://github.com/rdeneau/shopfoo/commit/03868df2303b488e8d717c83da5dad3e6505a5e9))
* 🐛 propagate lang change to the product pages ([b3a1d6a](https://github.com/rdeneau/shopfoo/commit/b3a1d6a8c35f186aab69cedc834931949567829b))
* 🐛 support book w/o covers ([970baae](https://github.com/rdeneau/shopfoo/commit/970baaef19ec2727993f1199d27affab55862f63))
* 🐛 support no prices, no sales, no stock events in workflows ([460a785](https://github.com/rdeneau/shopfoo/commit/460a78551e3ae5d84c9f5fe79301735db3bfb03a))
* 🐛 use div with key instead of fragment to avoid dreaded React error ([8e39c57](https://github.com/rdeneau/shopfoo/commit/8e39c57a9d7b5230644f284158f1e96acea097ea))
* 🐛 use Uri.Relative in Data Clients ([8380634](https://github.com/rdeneau/shopfoo/commit/838063435daf249e1be18261f4813aa0535695fd))
* 🔒️ encrypt tokens ([39b0093](https://github.com/rdeneau/shopfoo/commit/39b0093ef29d2d439642f364be6ce7f07326250f))
* **Action/MarkAsSoldOut:** 🐛 always trigger setSoldOut to apply the "SOLD-OUT" ribbon effect ([9fd08f5](https://github.com/rdeneau/shopfoo/commit/9fd08f5d23b790e8e30a0da579d58eccf6d7a08c))
* **Actions:** 🐛 setSoldOut in an effect to avoid React errors ([8c8a744](https://github.com/rdeneau/shopfoo/commit/8c8a7442cd399398b94c1db50ff6408f40025594))
* **Checkbox:**  🐛 label should not be focusable for better a11y / keyboard xp ([3e19310](https://github.com/rdeneau/shopfoo/commit/3e193107b2c3fecf0fb243c1e498652cc8eed4f0))
* **errorDetail:** 🐛 format manually the exception, fix missing keys ([7afeffa](https://github.com/rdeneau/shopfoo/commit/7afeffa1303d6f2332a4e3870801b157e8767589))
* **MultiSelect:**  🐛 after search, "select all" means all visible items ([4a61fab](https://github.com/rdeneau/shopfoo/commit/4a61fabcf8f00d8b112a96726002d5b530b2fc6b))
* **MultiSelect:**  🐛 after search, only show matches or selected items ([3365bba](https://github.com/rdeneau/shopfoo/commit/3365bba780678db4f494020e2246e18dfeae63fb))
* **MultiSelect:**  🐛clear button should not close the dropdown menu ([432788e](https://github.com/rdeneau/shopfoo/commit/432788edfc60cbe2be9d523e2ffcfe3378d7a850))
* **MultiSelect:** 🖼️ focus the search input on dropdown menu opening ([c2058c0](https://github.com/rdeneau/shopfoo/commit/c2058c07cd95e747476df69d073407aab46d9c91))
* **Product/Guard:** 💄 properly set PropertyName and EntityName ([3b0ad26](https://github.com/rdeneau/shopfoo/commit/3b0ad26b7f73d6840d45ca39c96dace40cfeaa73))
* **ProductsTable:** 🐛 fix condition to detect book subtitle ([bfac3e6](https://github.com/rdeneau/shopfoo/commit/bfac3e6701833eceba920ab91cfca8852866b1ce))
* **ResetCache:** 🐛 add missing Admin endpoint + refactor apiHttpHandler ([e03eded](https://github.com/rdeneau/shopfoo/commit/e03eded055c411a612252adda796684e6689f1e6))
* **search:** 🐛 support author w/o photos ([c4d3d27](https://github.com/rdeneau/shopfoo/commit/c4d3d27affcb7350ad95eff2a3338791d12191e6))

## [1.2.0](https://github.com/rdeneau/shopfoo/compare/v1.1.0...v1.2.0) (2025-12-30)

### Features

* ✨ adjust stock after inventory ([9e6d0f4](https://github.com/rdeneau/shopfoo/commit/9e6d0f4160e0c49204cda3459e8dbd1178f8f88a))
* ✨ fetch stock ; verifyZeroStock in MarkAsSoldOut workflow ([a13beb9](https://github.com/rdeneau/shopfoo/commit/a13beb9f58f0798d4f3ec13c7dad80bc173ee79c))

## [1.1.0](https://github.com/rdeneau/shopfoo/compare/v1.0.0...v1.1.0) (2025-12-24)

### Features

* ✨ add commits.html for the GitHub page to embed in the Status page ([3782a2a](https://github.com/rdeneau/shopfoo/commit/3782a2a041fd1495bb4913d724e7d25b33d83394))
* ✨ implement "define list/retail price" using ManagePriceFrom (previously ModifyPrice) ([abef0cf](https://github.com/rdeneau/shopfoo/commit/abef0cf18f7a5a5db74915de683141ffd2feec73))
* ✨ implement "mark as sold-out" (missing checking stock = 0) ([a717e27](https://github.com/rdeneau/shopfoo/commit/a717e27de22dd551f4259d6c504228246e4deb79))
* ✨ implement "remove list price" ([a10e5b6](https://github.com/rdeneau/shopfoo/commit/a10e5b6b2e7e68e2e652b572ece696cf9565b86e))
* 🖼️ display a "sold-out" ribbon on the product image ([6a7082b](https://github.com/rdeneau/shopfoo/commit/6a7082b2ed69a9510096eab4269bb2ed4271ab43))
* 🖼️ handle broken image url (at the UI level only) ([65f10f6](https://github.com/rdeneau/shopfoo/commit/65f10f63eb32f7a2e453134d49ec686fc1edc155))
* 🖼️ highlight selected menu ([e385825](https://github.com/rdeneau/shopfoo/commit/e385825c3bb6337d0c289491ce7c3f07554158bd))
* 🖼️ improve product image rendering when broken or sold-out ([dd3df7c](https://github.com/rdeneau/shopfoo/commit/dd3df7cba5162ad8463493a3c17e0985bf439220))
* 🖼️ use fontawesome: login, user icon ([fe5b6a3](https://github.com/rdeneau/shopfoo/commit/fe5b6a3fabb8fe783bf9f63101391168ba190db6))
* 🖼️ use fontawesome: navbar ([bbb6338](https://github.com/rdeneau/shopfoo/commit/bbb6338dee4aff6cb918444c4dc1f410766bb146))
* 🖼️ use fontawesome: product details ([03d5dff](https://github.com/rdeneau/shopfoo/commit/03d5dffc939f0c0258e9d45d2caf6822ddf67361))

## 1.0.0 (2025-12-20)

### Features

*  👔 translate Product/Actions ([f41d884](https://github.com/rdeneau/shopfoo/commit/f41d88490457cae566d88744ca21a475009b1a26))
* ♻️ revamp Errors: -HttpApi, -Validation, +Guard, +Category ([c02927f](https://github.com/rdeneau/shopfoo/commit/c02927f4cc9518e9eacd0a5852ff3407eee8902c))
* ✨ add Core/Shopfoo.Effects ([eb47aa4](https://github.com/rdeneau/shopfoo/commit/eb47aa4ff7b73b26959c06944883ee35048f43ab))
* ✨ add Login and FullContext (🚧 ReactContext ✖️) ([d124c0b](https://github.com/rdeneau/shopfoo/commit/d124c0b65e3f4dbaa63c5e531ce6bc622636d996))
* ✨ add product image ([7ce1e5d](https://github.com/rdeneau/shopfoo/commit/7ce1e5d13287f05fcae5b39d728f0d710733b416))
* ✨ add products ([f99b6d6](https://github.com/rdeneau/shopfoo/commit/f99b6d6de60f4eb0198f05a90bc55e203b778036))
* ✨ add the "not found" page ([ba95a70](https://github.com/rdeneau/shopfoo/commit/ba95a7003ba5dda6f7d1f3d44e627832946c76aa))
* ✨ display product details ([db706a9](https://github.com/rdeneau/shopfoo/commit/db706a9cffbadfd0dc8bcdde9f3809dc6f3fe0c0))
* ✨ display real retail prices, in € or $ with the symbol position (right vs left) ([4d4dbcc](https://github.com/rdeneau/shopfoo/commit/4d4dbcc0c46e8deab960e8f2b13ec6dec6c3c385))
* ✨ improve Common project (iso SCM) ([70336ee](https://github.com/rdeneau/shopfoo/commit/70336eec36da8e6a2a7f41756614498d7f3006ae))
* ✨ increase/decrease prices, open/close drawer ([73a97d1](https://github.com/rdeneau/shopfoo/commit/73a97d138fe6622d4e68331bef89f6afa92e7407))
* ✨ save product ([95476d3](https://github.com/rdeneau/shopfoo/commit/95476d381b945baea2bb29d64b6cc1f6f9bf5742))
* 🏗️ add Server build target ([2efc00c](https://github.com/rdeneau/shopfoo/commit/2efc00cf299e7690c38482ee2152e0def394d3cc))
* 🏗️ add simple console logger ([b814935](https://github.com/rdeneau/shopfoo/commit/b814935382d7c2acdce5407a78b56099eddffdbe))
* 👔 add Admin page to test the user access ([b0f1bbc](https://github.com/rdeneau/shopfoo/commit/b0f1bbc790cbfc06cc80cfecb3668631f3d6a3c7))
* 👔 after login, navigate to product/index only from the Login page ([5c18b9d](https://github.com/rdeneau/shopfoo/commit/5c18b9d488084fa774bb405da49f700a5d9dd56a))
* 👔 validation product in SaveProduct workflow ([b63d926](https://github.com/rdeneau/shopfoo/commit/b63d92630e414989c90e196d82af94df0b66fdaf))
* 🔐 check access to product actions ([87361e5](https://github.com/rdeneau/shopfoo/commit/87361e57a7aec8b39beedddd268ac85f6e4dfc8a))
* 🔐 check access to Product pages, redirecting to NotFound when needed ([c74f9b4](https://github.com/rdeneau/shopfoo/commit/c74f9b4606f15c9572f82a1f986947c734024ec2))
* 🔐 check product edit access ([96f67a1](https://github.com/rdeneau/shopfoo/commit/96f67a128483764965431fa3fd2b55b9d5e5d92c))
* 🖼️ add 4 themes, display preview badges ([71de84f](https://github.com/rdeneau/shopfoo/commit/71de84f3b3d8f1d723a53c11d9204ccbf5492fb1))
* 🖼️ add Alert.apiError to display details for Admins ([867826b](https://github.com/rdeneau/shopfoo/commit/867826b3ab21690b42612984536afedddd247f71))
* 🖼️ add theme changer in the nav bar ([6704db9](https://github.com/rdeneau/shopfoo/commit/6704db91fd6894c8eef5a3f91a398f346607bce7))
* 🖼️ display toast after lang changed ([aef0b6c](https://github.com/rdeneau/shopfoo/commit/aef0b6ce6906a2e35436dfaef694a68480b32494))
* 🖼️ handle product not found nicely ([1eee2cc](https://github.com/rdeneau/shopfoo/commit/1eee2cca5ef2f9009a9bbeb5fe671cd76aad33ba))
* 🖼️ improve LangDropdown ([7ec18a2](https://github.com/rdeneau/shopfoo/commit/7ec18a259975530608aef65369e6ca6906a4dd57))
* 🖼️ improve theme preview, ⚡ add fast build commands ([887a929](https://github.com/rdeneau/shopfoo/commit/887a92999b9c38186e6551c375b647fdcddcd50a))
* 🖼️ light theme ([079f5b8](https://github.com/rdeneau/shopfoo/commit/079f5b8f4d061d75215400ad881d264d6d1cc5c6))
* 🖼️ replace page title with the breadcrumb inside the app-navbar ([0b1b718](https://github.com/rdeneau/shopfoo/commit/0b1b71850836815ffae67270988192af24b2f46d))
* 🖼️ revamp Login page: display user claims in a table ([bff952e](https://github.com/rdeneau/shopfoo/commit/bff952ed623436460c3d6cf974caae22694d1cc5))
* 🖼️ setup UI (🚧 no behaviors) ([c329945](https://github.com/rdeneau/shopfoo/commit/c3299459220b324e543320e2a713eb2cc01360cb))
* 🖼️ translate ThemeDropdown ([fd317ec](https://github.com/rdeneau/shopfoo/commit/fd317eca97a986b5b81a58c9f1c4ddefe2cd0d8a))
* 🖼️ use breadcrumb as page title ([2095524](https://github.com/rdeneau/shopfoo/commit/209552439d63d0d7c6fed969da043fd86c7172ab))
* 🖼️ use emoji and persona wording (instead of demo user) ([7a8dfa8](https://github.com/rdeneau/shopfoo/commit/7a8dfa822da581e17ab6a57fcbfe9a2a8b0cfb39))
* 🖼️ validate catalog info on the client side ([42116cb](https://github.com/rdeneau/shopfoo/commit/42116cbe0417218a813c7b058c2ee08f6825f65c))
* **about:** ✨ add badges with package info ([a68765f](https://github.com/rdeneau/shopfoo/commit/a68765fe3c6cdb90d0945907674c96f335658883))
* **Errors:** ✨ add Validation, improve Guard ([1b49641](https://github.com/rdeneau/shopfoo/commit/1b496410987de7036d7c56b00836681e354f7f28))
* implement GetAllowedTranslations, GetProducts ([c01b1ef](https://github.com/rdeneau/shopfoo/commit/c01b1ef57372c54b6b2da802dfa41a3f52c19a3e))

### Bug Fixes

*  🐛 display "Shopfoo > Products" properly in the AppNav (instead of just "Shopfoo") ([1d02288](https://github.com/rdeneau/shopfoo/commit/1d02288bfd1817a805436f294abb24826742d8c4))
* 🐚 replace remaining SAFEr (from the template) to Shopfoo (app name) ([0883fee](https://github.com/rdeneau/shopfoo/commit/0883feede3ae268243d7e6ed3b8da0a0e6dea530))
* 🐛 add missing React keys, fix change lang ([f0faaee](https://github.com/rdeneau/shopfoo/commit/f0faaeedc89310f0a007c7a5e2138065774dafcb))
* 🐛 clear the Toast once hidden ([b0c96db](https://github.com/rdeneau/shopfoo/commit/b0c96db529ed212856be18ca9942cab6b6843b72))
* 🐛 delay the nav to Login in About w/o translations, to avoid React error ([73fa23f](https://github.com/rdeneau/shopfoo/commit/73fa23f7883995493cc24186b06bf5ee3c0c6717))
* 🐛 really save the product ([0415993](https://github.com/rdeneau/shopfoo/commit/0415993b9ad8bc0eebd462ef31222850546d6fda))
* 🐛 refresh saved prices, ✨ cancellable closing countdown ([e8ec58d](https://github.com/rdeneau/shopfoo/commit/e8ec58d2c13145e0984c9c2026d63eef40bf8e12))
* 🐛 remove the focus to fix the menu hiding on mouse out ([415f5c3](https://github.com/rdeneau/shopfoo/commit/415f5c37ef95e3d5b06c9ca1e5ffab9304208d80))
* **ci:**  🏗️ fix update-release-date ([9672d23](https://github.com/rdeneau/shopfoo/commit/9672d235d22d7aeab1137b57045b69c5a75a769f))
* **View:** 🐛 separate pageToDisplayInline from access check ([2b20619](https://github.com/rdeneau/shopfoo/commit/2b20619f1ee3f38bddcd045b625acd4b2fe35320))

## [Changelog]

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
