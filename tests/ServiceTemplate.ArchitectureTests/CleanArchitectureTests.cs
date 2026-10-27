using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using ServiceTemplate.Api.Endpoints;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Orders;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ServiceTemplate.ArchitectureTests;

public class CleanArchitectureTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(
            typeof(Order).Assembly,
            typeof(OrderEndpoints).Assembly
        ).Build();

    [Fact]
    public void Domain_Entities_Should_Inherit_From_Entity_Or_AggregateRoot()
    {
        Classes().That().ResideInNamespace("ServiceTemplate.Domain.Orders")
            .And().AreNotEnums()
            .And().DoNotHaveNameMatching(".*(DomainEvent|ValueObject|Exception|Status).*")
            .And().AreNotAbstract()
            .Should().BeAssignableTo(typeof(Entity))
            .Check(Architecture);
    }
}
