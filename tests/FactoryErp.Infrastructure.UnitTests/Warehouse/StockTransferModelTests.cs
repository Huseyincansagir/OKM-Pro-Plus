using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Warehouse;

public sealed class StockTransferModelTests
{
    [Fact]
    public void Stock_transfer_uses_table_checks_indexes_and_row_version()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(StockTransferRecord))!;

        entity.GetTableName().Should().Be("stock_transfers");
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_stock_transfers_entered_quantity_positive");
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_stock_transfers_quantity_positive");
        entity.GetIndexes().Should().Contain(index =>
            index.Properties.Select(x => x.Name).SequenceEqual(new[]
            {
                nameof(StockTransferRecord.Status),
                nameof(StockTransferRecord.CreatedAt),
            }));
        entity.FindProperty(nameof(StockTransferRecord.RowVersion))!
            .IsConcurrencyToken.Should().BeTrue();
    }

    private static FactoryErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql("Host=localhost;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me")
            .Options;

        return new FactoryErpDbContext(options);
    }
}
