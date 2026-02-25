[<AutoOpen>]
module Shopfoo.Tests.Common.FsCheckArbs.Shopfoo

type ShopfooFsCheckProperty() =
    inherit
        TUnit.FsCheck.FsCheckPropertyAttribute(
            Arbitrary = [| // ↩
                typeof<CommonArbs>
                typeof<DomainArbs>
            |]
        )