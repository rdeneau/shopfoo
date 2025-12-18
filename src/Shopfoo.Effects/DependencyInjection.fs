module Shopfoo.Effects.DependencyInjection

open System.Diagnostics
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Shopfoo.Common
open Shopfoo.Effects.Interpreter
open Shopfoo.Effects.Interpreter.Monitoring
open Shopfoo.Effects.Metrics

[<Interface>]
type IInterpreterFactory =
    abstract member Create<'dom when 'dom :> IDomain> : domain: 'dom -> Interpreter<'dom>

[<AutoOpen>]
module private Implementation =
    let private logifyPlainAsync (logger: ILogger) funcName funcAsync x =
        async {
            logger.LogDebug $"start %s{funcName} with arg\n%A{x}"
            let! result = funcAsync x
            logger.LogDebug $"%s{funcName} finished with result\n%A{result}"
            return result
        }

    type MetricsLogger(logger: ILogger<MetricsLogger>) =
        interface IMetricsSender with
            member _.SendTimeAsync(funcName, status, stopwatch) =
                async {
                    let metricSegments = [
                        "stats.timers.shopfoo.outgoing"
                        funcName |> String.toKebab
                        status.ToString()
                        $"duration:{int stopwatch.Elapsed.TotalMilliseconds}|ms"
                    ]

                    logger.LogInformation($"""[Metrics] Sending '%s{String.concat "." metricSegments}'""")
                }

    type PipelineLogger(logger: ILogger) =
        interface IPipelineLogger with
            member _.LogPipeline name pipeline arg =
                logifyPlainAsync logger name pipeline arg

    type PipelineLoggerFactory(loggerFactory: ILoggerFactory) =
        interface IPipelineLoggerFactory with
            member _.CreateLogger categoryName =
                PipelineLogger(logger = loggerFactory.CreateLogger categoryName)

    type PipelineTimer(metricsSender: IMetricsSender) =
        member private this.timeAsync(funcName, funcAsync: _ -> Async<'response>, args, buildStatus: 'response -> MetricsStatus) =
            async {
                let stopwatch = Stopwatch.StartNew()

                try
                    let! response = funcAsync args
                    do! metricsSender.SendTimeAsync(funcName, buildStatus response, stopwatch)
                    return response
                with ex ->
                    do! metricsSender.SendTimeAsync(funcName, MetricsStatus.Failed None, stopwatch)
                    return reraisePreserveStackTrace ex
            }

        interface IPipelineTimer with
            member this.TimeCommand name pipeline args =
                this.timeAsync (name, pipeline, args, MetricsStatus.ofResult)

            member this.TimeQuery name pipeline args =
                this.timeAsync (name, pipeline, args, MetricsStatus.ofOptionExpected)

            member this.TimeQueryOptional name pipeline args =
                this.timeAsync (name, pipeline, args, MetricsStatus.ofOption)

    [<Sealed>]
    type InterpreterFactory(loggerFactory: IPipelineLoggerFactory, timer: IPipelineTimer) =
        interface IInterpreterFactory with
            member _.Create(domain) =
                Interpreter(domain, loggerFactory, timer)

type IServiceCollection with
    member services.AddEffects() =
        services
            .AddSingleton<IMetricsSender, MetricsLogger>()
            .AddSingleton<IPipelineLoggerFactory, PipelineLoggerFactory>()
            .AddSingleton<IPipelineTimer, PipelineTimer>()
            .AddSingleton<IInterpreterFactory, InterpreterFactory>()