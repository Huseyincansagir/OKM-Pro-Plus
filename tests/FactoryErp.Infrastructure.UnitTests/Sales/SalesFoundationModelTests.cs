using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Sales;

public sealed class SalesFoundationModelTests
{
    [Fact]
    public void Customer_has_unique_code_and_candidate_status_constraint()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(CustomerRecord))!;

        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name)
            .SequenceEqual(new[] { nameof(CustomerRecord.CustomerCode) }));
        entity.GetCheckConstraints().Should().Contain(constraint => constraint.Name == "ck_customers_status");
    }

    [Fact]
    public void Quote_request_has_request_number_unique_index_and_item_positive_constraints()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var request = model.FindEntityType(typeof(QuoteRequestRecord))!;
        var item = model.FindEntityType(typeof(QuoteRequestItemRecord))!;

        request.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name)
            .SequenceEqual(new[] { nameof(QuoteRequestRecord.RequestNumber) }));
        request.GetCheckConstraints().Should().Contain(constraint => constraint.Name == "ck_quote_requests_status");
        item.GetCheckConstraints().Should().Contain(constraint => constraint.Name == "ck_quote_request_items_base_positive");
    }

    [Fact]
    public void Sales_order_item_has_remaining_projection_and_quantity_guards()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(SalesOrderItemRecord))!;

        entity.FindProperty(nameof(SalesOrderItemRecord.OrderedQty))!.GetPrecision().Should().Be(18);
        entity.FindProperty(nameof(SalesOrderItemRecord.OrderedQty))!.GetScale().Should().Be(6);
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_sales_order_items_remaining_projection");
    }

    [Fact]
    public void Reservation_has_concurrency_token_and_component_upper_bound()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(StockReservationRecord))!;

        entity.FindProperty(nameof(StockReservationRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_stock_reservations_components_within_quantity");
    }

    [Fact]
    public void Approval_has_order_and_decision_indexes()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(SalesOrderApprovalRecord))!;

        entity.GetIndexes().Should().Contain(index => index.Properties.Select(x => x.Name)
            .SequenceEqual(new[] { nameof(SalesOrderApprovalRecord.SalesOrderId), nameof(SalesOrderApprovalRecord.DecidedAt) }));
    }

    private static FactoryErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql("Host=localhost;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me")
            .Options;

        return new FactoryErpDbContext(options);
    }
}
