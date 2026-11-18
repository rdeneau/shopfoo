# Fable.Remoting.Client (vendored)

Vendored copy of the [Fable.Remoting](https://github.com/Zaid-Ajaj/Fable.Remoting) client 8.0.0, embedding the Fable sources of Fable.SimpleJson 3.24.0 and Fable.Remoting.MsgPack 2.0.0, patched for Fable 5.

## Why

Fable 5.1+ reports options and lists as unions ([fable-compiler/Fable#4529](https://github.com/fable-compiler/Fable/pull/4529)).
Fable.SimpleJson 3.24.0 tests for unions before options, so the client encodes `None` as `"None"` instead of `null`.
The server then fails with a `KeyNotFoundException` on any request carrying a `None`, such as the `Token` of an anonymous user calling `HomeApi.Index` from the login page.
Fable.Remoting.MsgPack has the same issue with lists and options.

Upstream status:

- fix: [Zaid-Ajaj/Fable.SimpleJson#111](https://github.com/Zaid-Ajaj/Fable.SimpleJson/pull/111), still open;
- issue: [Zaid-Ajaj/Fable.Remoting#394](https://github.com/Zaid-Ajaj/Fable.Remoting/issues/394), still open.

## Provenance

| Folder                    | Upstream                                                                                                   | Files                                                                   |
| ------------------------- | ---------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| `.`                       | [Fable.Remoting `a60eff3`](https://github.com/Zaid-Ajaj/Fable.Remoting/tree/a60eff3/Fable.Remoting.Client) | `Types.fs`, `Extensions.fs`, `Http.fs`, `Proxy.fs`, `Remoting.fs`       |
| `Fable.Remoting.MsgPack/` | [Fable.Remoting `a60eff3`](https://github.com/Zaid-Ajaj/Fable.Remoting/tree/a60eff3/Fable.Remoting.MsgPack) | `TypeShape.fs`, `TypeShapeUtils.fs`, `Format.fs`, `Write.fs`, `Read.fs` |
| `Fable.SimpleJson/`       | [Fable.SimpleJson `9d71bb3`](https://github.com/Zaid-Ajaj/Fable.SimpleJson/tree/9d71bb3/src)               | `src/*.fs`, `isBrowser.js`, `quote.js`                                  |

The upstream files are identical to the `fable/` sources of the NuGet packages Fable.Remoting.Client 8.0.0, Fable.Remoting.MsgPack 2.0.0 and Fable.SimpleJson 3.24.0.
Fable.SimpleJson has no 3.24.0 tag: `9d71bb3` is the commit that bumped the version to 3.24.0, before the unpublished `TypeInfo.Task` support.

`Fable.Remoting.Client.fsproj` compiles the files in the order of the upstream projects: SimpleJson, then MsgPack, then the client.

## Patches

- decode options and lists with Fable 5.1+:
  - `Fable.SimpleJson/TypeInfo.Converter.fs`: match options, lists, sets and maps before unions;
  - `Fable.Remoting.MsgPack/Read.fs` and `Write.fs`: read and write lists and options before unions;
- remove the FS3548 pattern discards in `Fable.SimpleJson/Json.Converter.fs`: `TypeInfo.Guid` and `TypeInfo.Long` carry no data.

`Fable.Remoting.Client.fsproj` ignores FS0044: the .NET side of `TypeShape.fs` and `TypeShapeUtils.fs` uses formatter-based serialization APIs, obsolete since .NET 8.

## Licence

MIT for both upstream repositories, see [LICENSE](LICENSE) for Fable.Remoting and [Fable.SimpleJson/LICENSE](Fable.SimpleJson/LICENSE) for Fable.SimpleJson.

## Removal

Go back to the `Fable.Remoting.Client` NuGet package once a Fable.SimpleJson release ships Zaid-Ajaj/Fable.SimpleJson#111, along with a Fable.Remoting.MsgPack release fixing lists and options.
Delete this folder and its `Shopfoo.slnx` entry, restore `nuget Fable.Remoting.Client` in `paket.dependencies` and `Fable.Remoting.Client` in `src/Shopfoo.Client/paket.references`, then remove the `ProjectReference` from `Shopfoo.Client.fsproj`.
Also drop `nuget Fable.Browser.XMLHttpRequest` and `nuget Fable.Parsimmon` from `paket.dependencies`: they are only listed for this folder.
