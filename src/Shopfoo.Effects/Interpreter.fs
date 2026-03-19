module Shopfoo.Effects.Interpreter

open Shopfoo.Common
open Shopfoo.Domain.Types.Errors
open Shopfoo.Effects.Instruction

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

    member private _.Instruction
        (
            step: IInterpretableStep<'union>,
            instruction: Instruction<'req, 'res>,
            withTiming: string -> ('req -> Async<'res>) -> 'req -> Async<'res>,
            pipeline: 'req -> Async<'res>
        ) : Async<obj> =
        let pipelineWithMonitoring =
            pipeline // ↩
            |> logger.LogPipeline step.Name
            |> withTiming step.Name

        async {
            let! res = pipelineWithMonitoring instruction.Request
            return box res
        }

    member this.Command(step, command: Command<_>, pipeline) = this.Instruction(step, command, timer.TimeCommand, pipeline)

    member this.Query(step: IInterpretableStep<'union>, query: Query<'req, 'ret>, pipeline: 'req -> Async<'ret option>) =
        this.Instruction(step, query, timer.TimeQuery, pipeline)

    member this.QueryFailable(step, query: QueryFailable<_, _>, pipeline) = this.Instruction(step, query, timer.TimeCommand, pipeline)
    member this.QueryOptional(step, query: Query<_, _>, pipeline) = this.Instruction(step, query, timer.TimeQueryOptional, pipeline)

    member _.Workflow<'arg, 'ret, 'step, 'workflow
        when 'workflow :> IProgramWorkflow<'arg, 'ret>
        and 'workflow :> IDomainWorkflow<'dom>>
        (runStep: 'step -> Async<obj>)
        =
        let rec loop program =
            match program with
            | End res -> async { return res }
            | Step(As(step: 'step), cont) ->
                async {
                    let! res = runStep step
                    return! loop (cont res)
                }
            | Step(step, _) -> failwithf $"Unsupported step: %A{step}"

        fun (workflow: 'workflow) (arg: 'arg) ->
            async {
                try
                    let program = workflow.Run(arg)
                    return! loop program
                with FirstException exn ->
                    return bug exn
            }