module Shopfoo.Effects.Dependencies

open System.Diagnostics
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Shopfoo.Common
open Shopfoo.Effects.Monitoring
open Shopfoo.Effects.Metrics

[<Interface>]
type IDomainMonitorFactory =
    abstract member Create<'ins when 'ins :> IProgramInstructions> : domainName: string -> DomainMonitor<'ins>

[<AutoOpen>]
module private Implementation =
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

    type WorkLogger(logger: ILogger, ?logLevel: LogLevel) =
        let logLevel = defaultArg logLevel LogLevel.Debug

        interface IWorkLogger with
            member _.Logger() =
                WorkMonitor(fun name work arg ->
                    async {
                        logger.Log(logLevel, $"start %s{name}")
                        let! result = work arg
                        logger.Log(logLevel, $"%s{name} finished with result\n%A{result}")
                        return result
                    }
                )

    type WorkMonitors(loggerFactory: ILoggerFactory, metricsSender: IMetricsSender) =
        let timeAsync (buildStatus: 'response -> MetricsStatus) funcName (work: Work<'arg, 'response>) arg =
            async {
                let stopwatch = Stopwatch.StartNew()

                try
                    let! response = work arg
                    do! metricsSender.SendTimeAsync(funcName, buildStatus response, stopwatch)
                    return response
                with ex ->
                    do! metricsSender.SendTimeAsync(funcName, MetricsStatus.Failed None, stopwatch)
                    return reraisePreserveStackTrace ex
            }

        interface IWorkMonitors with
            member _.LoggerFactory categoryName = WorkLogger(logger = loggerFactory.CreateLogger categoryName)
            member _.CommandTimer() = WorkMonitor(timeAsync MetricsStatus.ofResult)
            member _.QueryTimer() = WorkMonitor(timeAsync MetricsStatus.ofOptionExpected)

    [<Sealed>]
    type DomainMonitorFactory(monitors: IWorkMonitors) =
        interface IDomainMonitorFactory with
            member _.Create(domainName) = DomainMonitor(domainName, monitors)

type IServiceCollection with
    member services.AddEffects() =
        services
            .AddSingleton<IMetricsSender, MetricsLogger>()
            .AddSingleton<IWorkMonitors, WorkMonitors>()
            .AddSingleton<IDomainMonitorFactory, DomainMonitorFactory>()