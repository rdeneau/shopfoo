namespace Shopfoo.Server.Tests

open System.Reflection
open Shopfoo.Feat.Tests
open Shopfoo.Tests.Common.NetArchTestExtensions
open NetArchTest.Rules
open TUnit.Core

type ArchitectureTests() =
    static let serverTypes = Types.InAssembly(Assembly.Load("Shopfoo.Server"))

    [<Test>]
    member _.``Remoting API request handlers should be sealed and in their dedicated file``() =
        serverTypes
            .That()
            .Inherit<Shopfoo.Server.Remoting.Security.SecureRequestHandler<_, _>>()
            .Should()
            .BeSealed()
            .And()
            .HaveSourceFileNameMatchingName()
            .Verify()

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Server project should not access Feat data layer``(feat: Feat) =
        serverTypes.Should().NotHaveDependencyOnAny($"{feat.RootNamespace}.Data").Verify()

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Server project should not access Feat internal elements using InternalsVisibleTo``(feat: Feat) =
        let workflows = feat.Workflows().GetTypes() |> Seq.map _.FullName |> Seq.toArray
        serverTypes.Should().NotHaveDependencyOnAny(workflows).Verify(allowEmptySelectedTypes = true)

    [<Test; MethodDataSource(typeof<FeatData>, "AllFeats")>]
    member _.``Server project should not access data DTOs, public just to prevent serialization issues``(feat: Feat) =
        let dtos = feat.DataDtos().GetTypes() |> Seq.map _.FullName |> Seq.toArray
        serverTypes.Should().NotHaveDependencyOnAny(dtos).Verify(allowEmptySelectedTypes = true)