module Shopfoo.Tests.Common.NetArchTestExtensions

open Mono.Cecil
open NetArchTest.Rules
open Swensen.Unquote

module FluentFSharp =
    let that (appendPredicate: Predicate -> PredicateList) (types: Types) : PredicateList = // ↩
        types.That() |> appendPredicate

    let and' (appendPredicate: Predicate -> PredicateList) (predicateList: PredicateList) : PredicateList = // ↩
        predicateList.And() |> appendPredicate

open FluentFSharp

type ConditionList with
    member this.Verify(?allowEmptySelectedTypes) =
        let allowEmptySelectedTypes = defaultArg allowEmptySelectedTypes false
        let testResult = this.GetResult()
        let failingTypes = [ for t in testResult.FailingTypes -> t.FullName ]
        let selectedTypes = [ for t in testResult.SelectedTypesForTesting -> t.FullName ]
        test <@ (failingTypes = []) && (selectedTypes <> [] || allowEmptySelectedTypes) @>

type TypeDefinition with
    member t.GetFilePath() =
        let urls =
            seq {
                if t.HasMethods then
                    for method in t.Methods do
                        if method.DebugInformation.HasSequencePoints then
                            for s in method.DebugInformation.SequencePoints do
                                s.Document.Url
            }

        urls |> Seq.head

    member t.GetNameWithoutGenericPart() =
        let name = t.Name
        let index = name.IndexOf('`')

        if index >= 0 then name.Substring(0, index) else name

    member t.IsCompiledAsFsharpModule() =
        t.CustomAttributes
        |> Seq.filter (fun c -> c.AttributeType.FullName = typeof<CompilationMappingAttribute>.FullName)
        |> Seq.collect _.ConstructorArguments
        |> Seq.exists (fun p ->
            p.Type.FullName = typeof<SourceConstructFlags>.FullName
            && (p.Value = box (int SourceConstructFlags.Module) || p.Value = box SourceConstructFlags.Module)
        )

type Predicate with
    /// Checks if the type is an F# module, i.e. a static class with the `CompilationMapping(SourceConstructFlags.Module)` attribute.
    member this.AreFsharpModules() =
        this.AreClasses() // ↩
        |> and' _.AreStatic()
        |> and' _.MeetCustomRule(_.IsCompiledAsFsharpModule())

type HaveSourceFileMatchingNameWithoutSuffix(suffix: string) =
    static member Workflow = HaveSourceFileMatchingNameWithoutSuffix("Workflow")

    interface ICustomRule with
        member _.MeetsRule(typeDefinition: TypeDefinition) =
            let filePath = typeDefinition.GetFilePath()
            let fileName = System.IO.Path.GetFileNameWithoutExtension(filePath)
            let typeName = typeDefinition.GetNameWithoutGenericPart()
            $"{fileName}{suffix}" = typeName