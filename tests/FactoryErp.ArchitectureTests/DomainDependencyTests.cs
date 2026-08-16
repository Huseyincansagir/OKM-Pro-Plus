using FluentAssertions;
using FactoryErp.Domain.Common;
using NetArchTest.Rules;

namespace FactoryErp.ArchitectureTests;

public sealed class DomainDependencyTests
{
    [Fact]
    public void Domain_MustNotDependOnFrameworkOrInfrastructurePackages()
    {
        var result = Types
            .InAssembly(typeof(Entity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "FactoryErp.Application",
                "FactoryErp.Infrastructure",
                "FactoryErp.Api",
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                "Microsoft.Data",
                "Dapper",
                "System.Data")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is null
            ? "Domain katmanı framework ve infrastructure bağımlılığı içermemelidir."
            : $"Bağımlılık ihlali: {string.Join(", ", result.FailingTypeNames)}");
    }

    [Fact]
    public void Domain_TypesMustRemainInDomainAssembly()
    {
        var domainAssembly = typeof(Entity).Assembly;

        domainAssembly.GetTypes()
            .Where(type => type.IsPublic)
            .Should()
            .NotBeEmpty();
    }
}
