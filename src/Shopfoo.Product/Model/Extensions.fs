module internal Shopfoo.Product.Model.Extensions

open Shopfoo.Domain.Types.Errors

type Guard with
    member guard.Validate(criteria: GuardCriteria, value: string) =
        validation {
            let! _ = guard.Satisfies(value, criteria).ToValidation()
            return ()
        }