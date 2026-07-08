# Fable.Remoting `None` Deserialization Failure

## Summary

`feat/net-10-upgrade-experiments` fails on `POST /api/HomeApi/Index` because the client sends an anonymous auth token as the JSON string `"None"` instead of JSON `null`.

The server expects `Token: AuthToken option` to deserialize from `null` for `None`. When it receives `"None"`, `Fable.Remoting.Json` tries to resolve `None` as a union/discriminator key and throws:

```text
System.Collections.Generic.KeyNotFoundException:
The given key 'None' was not present in the dictionary.
```

## Why It Happens Only On Experiments

The remoting API contract and server handler code did not change between:

- `origin/feat/net-10-upgrade`
- `origin/feat/net-10-upgrade-experiments`

The important change is the client toolchain:

```diff
- "fable": { "version": "4.29.0" }
+ "fable": { "version": "5.4.0" }
```

The experiments branch also upgrades Feliz-related client dependencies, but the concrete serialization break is caused by the generated Fable 5 client output used by `Fable.Remoting.Client 8.0`.

## Evidence

Shared request contract:

```fsharp
type Request<'t> = {
    Token: AuthToken option
    Lang: Lang
    Body: 't
}
```

Client request creation:

```fsharp
member this.PrepareRequest body =
    let secureRequest: Request<'a> = {
        Token = this.Token
        Lang = this.Lang
        Body = body
    }

    this.Cmder, secureRequest
```

For anonymous users, `this.Token = None`.

Generated experiments client serialization was verified locally as:

```json
{"Token": "None", "Lang": "English", "Body": {"Query": null, "TranslationPages": []}}
```

The expected server-compatible payload is:

```json
{"Token": null, "Lang": "English", "Body": {"Query": null, "TranslationPages": []}}
```

## Failure Path

1. Login page loads home data.
2. Client calls `api.Home.Index`.
3. Anonymous context has `Token = None`.
4. Experiments client serializes `None` as `"None"`.
5. Server tries to deserialize `"None"` into `AuthToken option`.
6. `Fable.Remoting.Json` cannot resolve that shape and throws `KeyNotFoundException`.
7. Request returns HTTP 500 before `IndexHandler.Handle` performs useful work.

## Root Cause

`feat/net-10-upgrade-experiments` combines:

- Fable CLI/runtime `5.4.0`
- `Fable.Remoting.Client 8.0`
- `Fable.SimpleJson 3.24`
- `Fable.Remoting.Server 6.1`
- `Fable.Remoting.Json 3.0`

This combination serializes F# option `None` on the client as the union case name `"None"`, while the server-side remoting JSON converter expects option `None` to be represented as JSON `null`.

## Upstream Fable Commit

The Fable behavior change comes from:

- Commit: `d78a37db9f4c25eb51fac8afcd320b4ea36c60a7`
- Message: `fix(js/ts/python): Fix FSharpOption not recognized as union type in F# reflection (#4529)`
- Date: `2026-04-27`
- URL: <https://github.com/fable-compiler/Fable/commit/d78a37db9f4c25eb51fac8afcd320b4ea36c60a7>

This commit is included in Fable `5.1.0` and later, including `5.4.0`, `5.5.0`, and `5.6.0`. It is not included in Fable `4.29.0`.

The relevant change is in `src/fable-library-ts/Reflection.ts`. Before the commit, `option_type` had no union cases:

```ts
export function option_type(generic: TypeInfo): TypeInfo {
  return new TypeInfo("Microsoft.FSharp.Core.FSharpOption`1", [generic]);
}
```

After the commit, `option_type` explicitly exposes `None` and `Some` as union cases:

```ts
export function option_type(generic: TypeInfo): TypeInfo {
  const t: TypeInfo = new TypeInfo(
    "Microsoft.FSharp.Core.FSharpOption`1",
    [generic],
    undefined,
    undefined,
    undefined,
    () => [
      new CaseInfo(t, 0, "None"),
      new CaseInfo(t, 1, "Some", [["value", generic]])
    ]
  );
  return t;
}
```

That change is correct for F# reflection compatibility: `FSharpType.IsUnion(typeof<int option>)` should return `true`, and Fable added tests for that in the same commit.

The regression in this app is an interaction with `Fable.SimpleJson 3.24`: its type-info conversion checks `UnionType` before `OptionType`. Once Fable starts exposing `option` as a union, `Fable.SimpleJson` classifies `AuthToken option` as a normal union instead of as an option, causing `None` to serialize as `"None"` instead of `null`.

## Upstream Tracking

The same compatibility issue is tracked upstream in Fable.Remoting:

- Issue: `Option fields fail to serialize`
- URL: <https://github.com/Zaid-Ajaj/Fable.Remoting/issues/394>
- Opened: `2026-06-04`
- Relevant note: the issue reports that since Fable 5, return values containing `Option` fail because the client and server disagree about how options should be serialized.
- Repro package mentioned in the issue: `Fable.Remoting.Client 8.0.0`

The likely library-level fix is proposed in Fable.SimpleJson:

- PR: `Match against option types prior to union types`
- URL: <https://github.com/Zaid-Ajaj/Fable.SimpleJson/pull/111>
- Opened: `2026-06-09`
- Relevant change: match option types before union types in `TypeInfo.Converter.fs`.
- Why it matters here: with Fable 5, `option` now also satisfies union reflection. Matching `OptionType` first restores the old SimpleJson behavior where `None` is serialized as `null` instead of as the union case string `"None"`.

## Recommended Fixes

### Fast Confirmation / Temporary Fix

Pin Fable back to `4.29.0` on `feat/net-10-upgrade-experiments`, rebuild the client, and verify that the request payload uses `"Token": null`.

### Durable Fix

Align the Fable.Remoting client/server serialization stack with Fable 5 support. In practice, this means using a Fable.SimpleJson version that includes the fix from PR #111, once available, or applying an equivalent patch locally.

If no patched package is available, add a custom serialization workaround so F# option `None` is emitted as JSON `null` for remoting request DTOs.

## Files To Inspect

- `.config/dotnet-tools.json`
- `paket.lock`
- `src/Shopfoo.Shared/Remoting.fs`
- `src/Shopfoo.Client/Remoting.fs`
- `src/Shopfoo.Server/Remoting/Security.fs`
