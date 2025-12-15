namespace Shopfoo.Client

open Fable.Core
open Feliz
open Shopfoo.Shared.Remoting

type State<'t> = { current: 't; update: ('t -> 't) -> unit }

[<Erase>]
type private ReactContextComponent =
    [<ReactComponent>]
    static member Provider(context, defaultValue: 't, children) =
        let state = React.useStateWithUpdater defaultValue
        React.contextProvider (context, state, children = children)

type ReactContext<'t> [<ParamObject>] (name, defaultValue: 't) =
    // Fake function to set the context value, only used to define the context
    let fakeSetValue (_: 't -> 't) = ()

    let context =
        React.createContext (name, defaultValue = (defaultValue, fakeSetValue))

    /// <summary>
    /// Create a component providing the context to its <paramref name="children"/>.
    /// </summary>
    /// <param name="children">Elements inside the component.</param>
    /// <remarks>
    /// The <paramref name="children"/> only have to call the <c>Use()</c> method to get and set the context value.
    /// </remarks>
    member _.Provider(children) =
        ReactContextComponent.Provider(context, defaultValue, children)

    /// <summary>
    /// Wraps a <c>React.useContext</c> using the internal context.
    /// </summary>
    member _.Use() : State<'t> = // ↩
        let value, update = React.useContext context
        { current = value; update = update }

[<RequireQualifiedAccess>]
module ReactContexts =
    let FullContext = ReactContext(nameof FullContext, FullContext.Default)