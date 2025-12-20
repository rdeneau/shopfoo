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

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project setup
- SAFE Stack architecture with F# and React
- Azure deployment configuration
- Automated CI/CD workflows
