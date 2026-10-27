using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using ServiceTemplate.Application.Orders.CreateOrder;
using ServiceTemplate.Contracts.Events;
using ServiceTemplate.Domain.Orders;
using ServiceTemplate.Infrastructure.Persistence;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ServiceTemplate.ArchitectureTests;

public class BoundaryTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(
            typeof(Order).Assembly,                        // Domain
            typeof(CreateOrderCommand).Assembly,           // Application
            typeof(ApplicationDbContext).Assembly,         // Infrastructure
            typeof(OrderCreatedIntegrationEvent).Assembly, // Contracts
            typeof(Program).Assembly                       // Api
        ).Build();

    [Fact]
    public void Domain_Should_Not_Depend_On_Any_Other_Project()
    {
        Types().That().ResideInAssembly(typeof(Order).Assembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(typeof(CreateOrderCommand).Assembly)
                .Or().ResideInAssembly(typeof(ApplicationDbContext).Assembly)
                .Or().ResideInAssembly(typeof(OrderCreatedIntegrationEvent).Assembly)
                .Or().ResideInAssembly(typeof(Program).Assembly)
            ).Check(Architecture);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        Types().That().ResideInAssembly(typeof(CreateOrderCommand).Assembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(typeof(ApplicationDbContext).Assembly)
                .Or().ResideInAssembly(typeof(Program).Assembly)
            ).Check(Architecture);
    }

    [Fact]
    public void Contracts_Should_Have_No_Internal_Project_Dependencies()
    {
        Types().That().ResideInAssembly(typeof(OrderCreatedIntegrationEvent).Assembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(typeof(Order).Assembly)
                .Or().ResideInAssembly(typeof(CreateOrderCommand).Assembly)
                .Or().ResideInAssembly(typeof(ApplicationDbContext).Assembly)
                .Or().ResideInAssembly(typeof(Program).Assembly)
            ).Check(Architecture);
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        Types().That().ResideInAssembly(typeof(ApplicationDbContext).Assembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(typeof(Program).Assembly)
            ).Check(Architecture);
    }
}
