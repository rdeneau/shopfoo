[<AutoOpen>]
module Shopfoo.Client.Tests.FsCheckArbs.Shopfoo

type ShopfooFsCheckProperty() =
    inherit
        TUnit.FsCheck.FsCheckPropertyAttribute(
            Arbitrary = [| // ↩
                typeof<CommonArbs>
                typeof<DomainArbs>
            |]
        )