module Shopfoo.Program.Monitoring

open Shopfoo.Domain.Types.Errors

type Work<'arg, 'ret> = 'arg -> Async<'ret>
type WorkMonitor<'arg, 'ret> = delegate of name: string * work: Work<'arg, 'ret> -> Work<'arg, 'ret>

[<Interface>]
type IWorkLogger =
    abstract member Logger: unit -> WorkMonitor<'arg, 'ret>

[<Interface>]
type IWorkMonitors =
    abstract member LoggerFactory: categoryName: string -> IWorkLogger
    abstract member CommandTimer: unit -> WorkMonitor<'arg, Result<'ret, Error>>
    abstract member QueryTimer: unit -> WorkMonitor<'arg, 'ret option>

[<Sealed>]
type DomainMonitor<'ins when 'ins :> IProgramInstructions>(domainName: string, monitors: IWorkMonitors) =
    let loggerFactory = monitors.LoggerFactory(categoryName = $"Shopfoo.%s{domainName}.Workflow")

    let instruction (timerFactory: unit -> WorkMonitor<'arg, 'ret>) name (work: Work<'arg, 'ret>) : Work<'arg, 'ret> =
        let loggedWork = loggerFactory.Logger().Invoke(name, work)
        let timedWorks = timerFactory().Invoke(name, loggedWork)
        timedWorks

    member _.Query name work : Work<'arg, 'ret option> = instruction monitors.QueryTimer name work
    member _.Command name work : Work<'arg, Result<'ret, Error>> = instruction monitors.CommandTimer name work

    member _.Workflow (workflow: #IProgramWorkflow<'ins, 'arg, 'ret>) (arg: 'arg) (ins: 'ins) : Async<Result<'ret, Error>> =
        async {
            try
                return! workflow.Run arg ins
            with FirstException exn ->
                return bug exn
        }