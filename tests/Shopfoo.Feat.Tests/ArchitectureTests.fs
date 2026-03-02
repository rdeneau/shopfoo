module Shopfoo.Feat.Tests.ArchitectureTests

open System.Reflection
open System.Text.RegularExpressions
open Shopfoo.Tests.Common.NetArchTestExtensions
open Shopfoo.Tests.Common.NetArchTestExtensions.FluentFSharp
open NetArchTest.Rules
open TUnit.Core

[<AutoOpen>]
module Fixture =
    type Feat =
        | Product
        | Home

        member private this.ApiType =
            match this with
            | Product -> typeof<Shopfoo.Product.IProductApi>
            | Home -> typeof<Shopfoo.Home.IHomeApi>

        member this.RootNamespace = this.ApiType.Namespace

        member this.Types() = Types.InAssembly(this.ApiType.Assembly)

        member this.Workflows() =
            this.Types() // ↩
            |> that _.HaveNameEndingWith("Workflow")
            |> and' _.ResideInNamespaceMatching($"""{Regex.Escape this.RootNamespace}\.Workflows""")
            |> and' _.AreClasses()

        member private this.DataTypesWithNameEndingWith(suffix: string) =
            this.Types() // ↩
            |> that _.HaveNameEndingWith(suffix)
            |> and' _.ResideInNamespace($"{this.RootNamespace}.Data")

        member this.DataPipelines() =
            this.DataTypesWithNameEndingWith("Pipeline")
            |> and' _.AreClasses()
            |> and' _.AreNotAbstract()
            |> and' _.AreNotStatic()

        member this.DataClients() =
            this.DataTypesWithNameEndingWith("Client")
            |> and' _.AreClasses()
            |> and' _.AreNotAbstract()
            |> and' _.AreNotStatic()

        member this.DataDtos() =
            this.Types() // ↩
            |> that _.HaveNameMatching("^Dto$")
            |> and' _.ResideInNamespace($"{this.RootNamespace}.Data")
            |> and' _.AreFsharpModules()

        member this.DataMappers() = this.DataTypesWithNameEndingWith("Mappers") |> and' _.AreFsharpModules()

    module Feat =
        let All = [ Product; Home ]

    type FeatData =
        static member AllFeats() = [| Feat.Product; Feat.Home |]

type FeatArchitectureTests() =
    let otherNamespacesOf feat = [|
        for x in Feat.All do
            if x <> feat then
                x.RootNamespace
    |]

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Feat project should not reference other feat projects``(feat: Feat) =
        feat.Types().Should().NotHaveDependencyOnAny(otherNamespacesOf feat).Verify()

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Feat project should not reference the Server project``(feat: Feat) =
        feat.Types().Should().NotHaveDependencyOnAny("Shopfoo.Server").Verify()

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Domain types should not depend on feat projects``(feat: Feat) =
        Types.InAssembly(Assembly.Load("Shopfoo.Domain.Types")).Should().NotHaveDependencyOnAny(feat.RootNamespace).Verify()

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Feat data pipelines should be internal``(feat: Feat) =
        feat.DataPipelines().Should().BeInternal().Verify(allowEmptySelectedTypes = true)

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Feat data clients should be internal``(feat: Feat) = feat.DataClients().Should().BeInternal().Verify(allowEmptySelectedTypes = true)

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Feat data DTOs should be public to prevent serialization issues``(feat: Feat) =
        feat.DataDtos().Should().BePublic().Verify(allowEmptySelectedTypes = true)

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Feat data mappers should be internal``(feat: Feat) = feat.DataMappers().Should().BeInternal().Verify(allowEmptySelectedTypes = true)

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Workflow class name should end with Workflow``(feat: Feat) =
        feat.Workflows().Should().HaveNameEndingWith("Workflow").Verify(allowEmptySelectedTypes = true)

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Workflows should not depend on Data types``(feat: Feat) =
        feat.Workflows().Should().NotHaveDependencyOnAny($"{feat.RootNamespace}.Data").Verify(allowEmptySelectedTypes = true)

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Workflows should be sealed and internal classes``(feat: Feat) =
        feat.Workflows().Should().BeSealed().And().BeInternal().Verify(allowEmptySelectedTypes = true)

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Workflows should be in their dedicated file, named without the Workflow suffix``(feat: Feat) =
        feat.Workflows().Should().MeetCustomRule(HaveSourceFileMatchingNameWithoutSuffix.Workflow).Verify(allowEmptySelectedTypes = true)

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Workflows should not depend on data DTOs``(feat: Feat) =
        let dtos = feat.DataDtos().GetTypes() |> Seq.map _.FullName |> Seq.toArray
        feat.Workflows().Should().NotHaveDependencyOnAny(dtos).Verify(allowEmptySelectedTypes = true)