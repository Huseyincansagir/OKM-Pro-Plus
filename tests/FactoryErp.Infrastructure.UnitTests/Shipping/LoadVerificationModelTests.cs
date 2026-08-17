using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class LoadVerificationModelTests
{
    [Fact]
    public void Sessions_have_status_completion_checks_active_plan_index_and_row_version()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(LoadVerificationSessionRecord))!;

        entity.GetTableName().Should().Be("load_verification_sessions");
        entity.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_load_verification_session_status",
            "ck_load_verification_session_completion_pair",
            "ck_load_verification_session_discrepancy_reason",
        });
        entity.FindProperty(nameof(LoadVerificationSessionRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        entity.GetIndexes().Should().Contain(x => x.IsUnique
            && x.GetFilter() == "status in ('Draft', 'InProgress')"
            && x.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(LoadVerificationSessionRecord.LoadPlanId) }));
    }

    [Fact]
    public void Scans_have_status_mode_quantity_key_checks_and_filtered_package_uniqueness()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(LoadVerificationScanRecord))!;

        entity.GetTableName().Should().Be("load_verification_scans");
        entity.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_load_verification_scan_status",
            "ck_load_verification_scan_mode",
            "ck_load_verification_scan_barcode",
            "ck_load_verification_scan_quantity",
            "ck_load_verification_scan_accepted_package",
            "ck_load_verification_scan_keys",
        });
        entity.FindProperty(nameof(LoadVerificationScanRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        entity.GetIndexes().Should().Contain(x => x.IsUnique
            && x.GetFilter() == "status = 'Accepted' and shipment_package_id is not null"
            && x.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(LoadVerificationScanRecord.SessionId), nameof(LoadVerificationScanRecord.ShipmentPackageId),
            }));
        entity.GetIndexes().Should().Contain(x => x.IsUnique
            && x.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(LoadVerificationScanRecord.SessionId), nameof(LoadVerificationScanRecord.IdempotencyKey),
            }));
    }

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new FactoryErpDbContext(options);
    }
}
