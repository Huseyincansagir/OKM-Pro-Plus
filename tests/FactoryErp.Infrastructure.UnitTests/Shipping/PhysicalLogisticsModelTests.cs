using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class PhysicalLogisticsModelTests
{
    [Fact]
    public void ProductPhysicalProfile_uses_effective_profile_unique_index_and_checks()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(ProductPhysicalProfileRecord))!;

        entity.GetTableName().Should().Be("product_physical_profiles");
        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name).SequenceEqual(new[]
        {
            nameof(ProductPhysicalProfileRecord.ProductId), nameof(ProductPhysicalProfileRecord.EffectiveFrom)
        }));
        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_product_physical_dimensions_positive");
        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_product_physical_effective_range");
    }

    [Fact]
    public void PackagingPhysicalProfile_maps_json_policy_and_precision()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(PackagingPhysicalProfileRecord))!;

        entity.FindProperty(nameof(PackagingPhysicalProfileRecord.UnitsPerPackage))!.GetPrecision().Should().Be(18);
        entity.FindProperty(nameof(PackagingPhysicalProfileRecord.UnitsPerPackage))!.GetScale().Should().Be(6);
        entity.FindProperty(nameof(PackagingPhysicalProfileRecord.PhysicalPolicySnapshot))!.GetColumnType().Should().Be("jsonb");
        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_packaging_physical_gross_consistent");
    }

    [Fact]
    public void PalletType_uses_unique_code_and_payload_constraint()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(PalletTypeRecord))!;

        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name).SequenceEqual(new[] { nameof(PalletTypeRecord.Code) }));
        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_pallet_payload_not_over_gross");
    }

    [Fact]
    public void VehicleCapacityPhysicalRelations_have_composite_key_and_zone_order_index()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var join = model.FindEntityType(typeof(VehicleCapacityPalletTypeRecord))!;
        var zone = model.FindEntityType(typeof(VehicleCapacityZoneRecord))!;

        join.FindPrimaryKey()!.Properties.Select(x => x.Name).Should().Equal(nameof(VehicleCapacityPalletTypeRecord.VehicleCapacityId), nameof(VehicleCapacityPalletTypeRecord.PalletTypeId));
        zone.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name).SequenceEqual(new[]
        {
            nameof(VehicleCapacityZoneRecord.VehicleCapacityId), nameof(VehicleCapacityZoneRecord.SequenceNo)
        }));
    }

    private static FactoryErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql("Host=localhost;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me")
            .Options;
        return new FactoryErpDbContext(options);
    }
}
