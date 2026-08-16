using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Infrastructure;
using FluentAssertions;
using NetArchTest.Rules;
using System.Reflection;

namespace FactoryErp.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Application_MustNotDependOnInfrastructureApiOrFrameworkAdapters()
    {
        var result = Types
            .InAssembly(typeof(IUnitOfWork).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "FactoryErp.Infrastructure",
                "FactoryErp.Api",
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is null
            ? "Application katmanı Infrastructure, API veya framework adapter bağımlılığı taşımamalıdır."
            : $"Application bağımlılık ihlali: {string.Join(", ", result.FailingTypeNames)}");
    }

    [Fact]
    public void Infrastructure_MustNotDependOnApi()
    {
        var result = Types
            .InAssembly(typeof(DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOn("FactoryErp.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is null
            ? "Infrastructure katmanı API’ye bağımlı olmamalıdır."
            : $"Infrastructure bağımlılık ihlali: {string.Join(", ", result.FailingTypeNames)}");
    }

    [Fact]
    public void Api_MayDependOnApplicationAndInfrastructureButNotTheReverse()
    {
        var apiAssembly = Assembly.Load("FactoryErp.Api");

        apiAssembly.GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Should()
            .Contain(new[] { "FactoryErp.Application", "FactoryErp.Infrastructure" });
    }
}
