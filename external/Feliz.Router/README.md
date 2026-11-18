# Feliz.Router (vendored)

Vendored copy of [Feliz.Router](https://github.com/Zaid-Ajaj/Feliz.Router) 4.0.0, patched for Feliz 3.

## Why

The `Feliz.Router` 4.0.0 package, the latest one on NuGet, targets Feliz 2.
Under Feliz 3, Fable fails to compile it: `React.useCallbackRef`, `React.createDisposable` and `React.fragment` no longer exist.

## Provenance

- `Router.fs`: upstream `src/Router.fs` at commit [`9ea3518`](https://github.com/Zaid-Ajaj/Feliz.Router/tree/9ea3518/src), identical to the sources of the 4.0.0 package.
- Patch: taken verbatim from the pull request [Zaid-Ajaj/Feliz.Router#72 "Update for Feliz 3"](https://github.com/Zaid-Ajaj/Feliz.Router/pull/72), head commit `0419eed`:
  - local `useCallbackRef` hook replacing `React.useCallbackRef`;
  - `FsReact.createDisposable` replacing `React.createDisposable`;
  - `React.memoRender` to render the memoized router;
  - `React.Fragment` replacing `React.fragment`;
  - `#nowarn "3397"` on `history.pushState`.
- `Feliz.Router.fsproj`: written for this repository, compiling `Router.fs` only.

## Licence

MIT, see [LICENSE](LICENSE), copied from the upstream repository.

## Removal

Go back to the `Feliz.Router` NuGet package once a release supports Feliz 3, for instance after Zaid-Ajaj/Feliz.Router#72 is merged and published.
Delete this folder and its `Shopfoo.slnx` entry, restore `nuget Feliz.Router` in `paket.dependencies` and `Feliz.Router` in `src/Shopfoo.Client/paket.references`, then remove the `ProjectReference` from `Shopfoo.Client.fsproj`.
