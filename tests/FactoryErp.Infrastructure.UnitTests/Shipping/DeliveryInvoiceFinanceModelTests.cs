using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class DeliveryInvoiceFinanceModelTests
{
    [Fact]
    public void Delivery_note_item_has_allocation_projection_guards()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(DeliveryNoteItemRecord))!;

        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_delivery_note_items_invoiced_within_shipped");
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_delivery_note_items_remaining_projection");
        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name)
            .SequenceEqual(new[] { nameof(DeliveryNoteItemRecord.DeliveryNoteId), nameof(DeliveryNoteItemRecord.SalesOrderItemId) }));
    }

    [Fact]
    public void Delivery_note_allocation_has_positive_quantity_and_unique_idempotency_key()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(DeliveryNoteItemAllocationRecord))!;

        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_delivery_note_allocations_quantity_positive");
        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name)
            .SequenceEqual(new[] { nameof(DeliveryNoteItemAllocationRecord.IdempotencyKey) }));
    }

    [Fact]
    public void Invoice_allocation_has_positive_quantity_and_unique_idempotency_key()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(InvoiceItemAllocationRecord))!;

        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_invoice_allocations_quantity_positive");
        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name)
            .SequenceEqual(new[] { nameof(InvoiceItemAllocationRecord.IdempotencyKey) }));
    }

    [Fact]
    public void Current_transaction_requires_exactly_one_positive_side()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(CurrentTransactionRecord))!;

        entity!.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_current_transactions_amounts_non_negative");
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_current_transactions_one_side");
    }

    [Fact]
    public void Current_account_is_unique_per_customer_and_currency_projection_is_precise()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(CurrentAccountRecord))!;

        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name)
            .SequenceEqual(new[] { nameof(CurrentAccountRecord.CustomerId) }));
        entity.FindProperty(nameof(CurrentAccountRecord.Balance))!.GetPrecision().Should().Be(18);
        entity.FindProperty(nameof(CurrentAccountRecord.Balance))!.GetScale().Should().Be(2);
    }

    private static FactoryErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql("Host=localhost;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me")
            .Options;

        return new FactoryErpDbContext(options);
    }
}
