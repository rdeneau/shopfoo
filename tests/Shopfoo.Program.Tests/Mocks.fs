module Shopfoo.Program.Tests.Mocks

open Microsoft.Extensions.DependencyInjection
open Shopfoo.Program.Dependencies
open Shopfoo.Program.Runner

type WorkMonitorCall = {
    Name: string
    Arg: obj
    Result: obj
}

module private WorkMonitorCall =
    let create name (arg: 'arg) (result: 'ret) = {
        Name = name
        Arg = box arg
        Result = box result
    }

type private WorkMonitorSpy() =
    let mutable calls: WorkMonitorCall list = []
    member _.Calls = calls

    member _.Object() =
        WorkMonitor<'arg, 'ret>(fun name work ->
            fun (arg: 'arg) ->
                async {
                    let! (result: 'ret) = work arg
                    calls <- (WorkMonitorCall.create name arg result) :: calls
                    return result
                }
        )

type private ConsoleWorkMonitor() =
    member _.Object() =
        WorkMonitor<'arg, 'ret>(fun name work ->
            fun (arg: 'arg) ->
                async {
                    let! (result: 'ret) = work arg
                    printfn $"[Tests] %s{name} with argument %A{arg} finished with result %A{result}"
                    return result
                }
        )

type WorkMonitorsMock() =
    let loggerWorkMonitor = ConsoleWorkMonitor()
    let queryTimerWorkMonitor = WorkMonitorSpy()
    let commandTimerWorkMonitor = WorkMonitorSpy()

    member _.Calls = {| QueryTimer = queryTimerWorkMonitor.Calls; CommandTimer = commandTimerWorkMonitor.Calls |}

    interface IWorkMonitors with
        member _.QueryTimer() = queryTimerWorkMonitor.Object()
        member _.CommandTimer() = commandTimerWorkMonitor.Object()

        member _.LoggerFactory _ =
            { new IWorkLogger with
                member _.Logger() = loggerWorkMonitor.Object()
            }

type IServiceCollection with
    member services.AddProgramMocks() =
        services
            // Production dependencies
            .AddProgram()
            // Test dependencies
            .AddSingleton<IWorkMonitors, WorkMonitorsMock>()