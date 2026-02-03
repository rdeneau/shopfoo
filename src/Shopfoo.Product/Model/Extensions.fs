module internal Shopfoo.Product.Model.Extensions

open Shopfoo.Domain.Types.Errors

type GuardCriteria with
    member guard.Validate(value) =
        validation {
            let! _ = Guard(nameof guard).Satisfies(value, guard).ToValidation()
            return ()
        }