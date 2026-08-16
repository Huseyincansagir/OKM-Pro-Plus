using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shared;
using FactoryErp.Domain.Shipping;

namespace FactoryErp.Domain.UnitTests.Shipping;

public sealed class DeliveryNoteItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void InvoiceAllocation_UpdatesRemainingToInvoice()
    {
        var item = CreateDeliveryNoteItem();

        item.AllocateInvoice(PositiveQuantity.Create(5_000, 0), Now.AddMinutes(1));

        Assert.Equal(5_000, item.InvoicedQuantity.BaseValue);
        Assert.Equal(5_000, item.RemainingToInvoice.BaseValue);
    }

    [Fact]
    public void InvoiceAllocationBeyondShippedQuantityIsRejected()
    {
        var item = CreateDeliveryNoteItem();

        var exception = Assert.Throws<DomainException>(() =>
            item.AllocateInvoice(PositiveQuantity.Create(10_001, 0), Now));

        Assert.Equal("OVER_INVOICING", exception.Error.Code);
    }

    [Fact]
    public void WaiverCannotExceedInvoiceableQuantity()
    {
        var item = CreateDeliveryNoteItem();

        var exception = Assert.Throws<DomainException>(() =>
            item.WaiveInvoiceableQuantity(PositiveQuantity.Create(10_001, 0), Now));

        Assert.Equal("WAIVER_EXCEEDS_REMAINING", exception.Error.Code);
    }

    [Fact]
    public void AllocationReversalRequiresReasonAndChangesStatus()
    {
        var snapshot = CreateSnapshot();
        var allocation = DeliveryNoteItemAllocation.Create(
            Guid.NewGuid(),
            Now,
            Guid.NewGuid(),
            Guid.NewGuid(),
            PositiveQuantity.Create(5_000, 0),
            snapshot);

        var missingReason = Assert.Throws<DomainException>(() => allocation.Reverse(Guid.NewGuid(), " ", Now.AddMinutes(1)));
        Assert.Equal("REVERSAL_REASON_REQUIRED", missingReason.Error.Code);

        var reversalId = Guid.NewGuid();
        allocation.Reverse(reversalId, "Depo sayım düzeltmesi", Now.AddMinutes(2));

        Assert.Equal(AllocationStatus.Reversed, allocation.Status);
        Assert.Equal(reversalId, allocation.ReversedFromId);
        Assert.Equal("Depo sayım düzeltmesi", allocation.ReversalReason);
    }

    [Fact]
    public void ReversingAlreadyReversedAllocationIsRejected()
    {
        var allocation = DeliveryNoteItemAllocation.Create(
            Guid.NewGuid(),
            Now,
            Guid.NewGuid(),
            Guid.NewGuid(),
            PositiveQuantity.Create(5_000, 0),
            CreateSnapshot());
        allocation.Reverse(Guid.NewGuid(), "İlk reversal", Now.AddMinutes(1));

        var exception = Assert.Throws<DomainException>(() => allocation.Reverse(Guid.NewGuid(), "İkinci reversal", Now.AddMinutes(2)));

        Assert.Equal("ALLOCATION_NOT_ACTIVE", exception.Error.Code);
    }

    private static DeliveryNoteItem CreateDeliveryNoteItem()
        => DeliveryNoteItem.Create(
            Guid.NewGuid(),
            Now,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PositiveQuantity.Create(10_000, 0));

    private static QuantitySnapshot CreateSnapshot()
    {
        var packaging = PackagingSnapshot.Create(
            Guid.NewGuid(),
            "Koli",
            "Koli",
            UomCode.Create("piece"),
            2_000,
            allowPartial: false,
            "v1");

        return new QuantitySnapshot(
            5,
            packaging.PackagingId,
            PositiveQuantity.Create(10_000, 0),
            UomCode.Create("piece"),
            "Packaging",
            packaging,
            []);
    }

    [Fact]
    public void AllocationWithinSourceAndPlannedBoundsIsAccepted()
    {
        var item = CreateDeliveryNoteItem();
        var allocation = DeliveryNoteItemAllocation.Create(
            Guid.NewGuid(),
            Now,
            item.SalesOrderItemId,
            item.Id,
            PositiveQuantity.Create(4_000, 0),
            CreateSnapshot());

        item.AddAllocation(allocation, NonNegativeQuantity.Create(10_000, 0), Now.AddMinutes(1));

        Assert.Equal(4_000, item.ActiveAllocatedQuantity().BaseValue);
        Assert.Single(item.Allocations);
    }

    [Fact]
    public void OverAllocation_IsRejectedWithTypedError()
    {
        var item = CreateDeliveryNoteItem();
        var first = DeliveryNoteItemAllocation.Create(
            Guid.NewGuid(),
            Now,
            item.SalesOrderItemId,
            item.Id,
            PositiveQuantity.Create(7_000, 0),
            CreateSnapshot());
        item.AddAllocation(first, NonNegativeQuantity.Create(10_000, 0), Now.AddMinutes(1));

        var second = DeliveryNoteItemAllocation.Create(
            Guid.NewGuid(),
            Now,
            item.SalesOrderItemId,
            item.Id,
            PositiveQuantity.Create(4_000, 0),
            CreateSnapshot());

        var exception = Assert.Throws<DomainException>(() =>
            item.AddAllocation(second, NonNegativeQuantity.Create(10_000, 0), Now.AddMinutes(2)));

        Assert.Equal("OVER_ALLOCATION", exception.Error.Code);
        Assert.Equal(7_000, item.ActiveAllocatedQuantity().BaseValue);
    }

    [Fact]
    public void ReversalIsPositiveRecordAndReferencesOriginalAllocation()
    {
        var original = DeliveryNoteItemAllocation.Create(
            Guid.NewGuid(),
            Now,
            Guid.NewGuid(),
            Guid.NewGuid(),
            PositiveQuantity.Create(3_000, 0),
            CreateSnapshot());

        var reversal = DeliveryNoteItemAllocation.CreateReversal(
            Guid.NewGuid(),
            Now.AddMinutes(1),
            original,
            "Sevkiyat düzeltmesi");

        Assert.Equal(3_000, reversal.QuantityBase.BaseValue);
        Assert.True(reversal.QuantityBase.BaseValue > 0);
        Assert.Equal(original.Id, reversal.ReversedFromId);
        Assert.Equal(AllocationStatus.Active, reversal.Status);
    }
}
