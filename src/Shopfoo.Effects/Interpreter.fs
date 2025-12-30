module Shopfoo.Effects.Interpreter

open Shopfoo.Domain.Types.Errors

module Monitoring =
    [<Interface>]
    type IPipelineLogger =
        abstract member LogPipeline: name: string -> pipeline: ('arg -> Async<'ret>) -> 'arg -> Async<'ret>

    [<Interface>]
    type IPipelineLoggerFactory =
        abstract member CreateLogger: categoryName: string -> IPipelineLogger

    [<Interface>]
    type IPipelineTimer =
        abstract member TimeCommand: name: string -> pipeline: ('arg -> Async<Result<'ret, Error>>) -> 'arg -> Async<Result<'ret, Error>>
        abstract member TimeQuery: name: string -> pipeline: ('arg -> Async<Option<'ret>>) -> 'arg -> Async<Option<'ret>>
        abstract member TimeQueryOptional: name: string -> pipeline: ('arg -> Async<Option<'ret>>) -> 'arg -> Async<Option<'ret>>

open Monitoring

[<Sealed>]
type Interpreter<'dom when 'dom :> IDomain>(domain: 'dom, loggerFactory: IPipelineLoggerFactory, timer: IPipelineTimer) =
    let logger = loggerFactory.CreateLogger $"Shopfoo.Domain.%s{domain.Name}.Workflow"

    member private _.Instruction(instruction: Instruction<_, _, _>, withTiming, pipeline: 'arg -> Async<_>) =
        let pipelineWithMonitoring =
            pipeline |> logger.LogPipeline instruction.Name |> withTiming instruction.Name

        instruction.RunAsync(pipelineWithMonitoring)

    member this.Command(command: Command<_, _>, pipeline) =
        this.Instruction(command, timer.TimeCommand, pipeline)

    member this.Query(query: Query<_, _, _>, pipeline) =
        this.Instruction(query, timer.TimeQuery, pipeline)

    member this.QueryFailable(query: QueryFailable<_, _, _>, pipeline) =
        this.Instruction(query, timer.TimeCommand, pipeline)

    member this.QueryOptional(query: Query<_, _, _>, pipeline) =
        this.Instruction(query, timer.TimeQueryOptional, pipeline)

    member _.Workflow<'arg, 'ret, 'effect, 'workflow
        when 'effect :> IProgramEffect<Program<Result<'ret, Error>>>
        and 'workflow :> IProgramWorkflow<'arg, 'ret>
        and 'workflow :> IDomainWorkflow<'dom>>
        runEffect
        =
        let rec loop program =
            match program with
            | Stop res -> async { return res }
            | Effect eff ->
                match eff with
                | :? 'effect as effect ->
                    async {
                        let! res = runEffect effect
                        return! loop res
                    }
                | _ -> failwithf $"Unsupported effect: %A{eff}"

        fun (workflow: 'workflow) (arg: 'arg) ->
            async {
                try
                    let program = workflow.Run(arg)
                    return! loop program
                with FirstException exn ->
                    return bug exn
            }