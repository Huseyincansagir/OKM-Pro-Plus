using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shared;
using FactoryErp.Domain.Warehouse;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Warehouse;

public sealed class StockTransferTests
{
    private static readonly Guid ProductId = Guid.Parse("40000000-0000-0000-0000-000000000101");
    private static readonly Guid SourceWarehouseId = Guid.Parse("40000000-0000-0000-0000-000000000102");
    private static readonly Guid SourceLocationId = Guid.Parse("40000000-0000-0000-0000-000000000103");
    private static readonly Guid TargetWarehouseId = Guid.Parse("40000000-0000-0000-0000-000000000104");
    private static readonly Guid TargetLocationId = Guid.Parse("40000000-0000-0000-0000-000000000105");
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_accepts_positive_transfer()
    {
        var transfer = CreateTransfer();

        transfer.Status.Should().Be(StockTransferStatus.Draft);
        transfer.QuantityBase.BaseValue.Should().Be(10_000m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_zero_or_negative_entered_quantity(decimal enteredQuantity)
    {
        var action = () => StockTransfer.Create(
            Guid.NewGuid(),
            Now,
            ProductId,
            SourceWarehouseId,
            SourceLocationId,
            TargetWarehouseId,
            TargetLocationId,
            enteredQuantity,
            null,
            "Packaging",
            PositiveQuantity.Create(10_000m, 6),
            "{}");

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("TRANSFER_ENTERED_QUANTITY_INVALID");
    }

    [Fact]
    public void Create_rejects_same_source_and_target_location()
    {
        var action = () => StockTransfer.Create(
            Guid.NewGuid(),
            Now,
            ProductId,
            SourceWarehouseId,
            SourceLocationId,
            SourceWarehouseId,
            SourceLocationId,
            1,
            null,
            "BaseUnit",
            PositiveQuantity.Create(1, 6),
            "{}");

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("TRANSFER_SOURCE_TARGET_SAME");
    }

    [Fact]
    public void Complete_accepts_draft_once()
    {
        var transfer = CreateTransfer();

        transfer.Complete(Now.AddMinutes(1));

        transfer.Status.Should().Be(StockTransferStatus.Completed);
        transfer.CompletedAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void Complete_rejects_replay_as_invalid_transition()
    {
        var transfer = CreateTransfer();
        transfer.Complete(Now);

        var action = () => transfer.Complete(Now.AddMinutes(1));

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("TRANSFER_INVALID_TRANSITION");
    }

    [Fact]
    public void Cancel_accepts_draft_and_rejects_completed_transfer()
    {
        var transfer = CreateTransfer();
        transfer.Cancel(Now);
        transfer.Status.Should().Be(StockTransferStatus.Cancelled);

        var completed = CreateTransfer();
        completed.Complete(Now);
        var action = () => completed.Cancel(Now.AddMinutes(1));

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("TRANSFER_INVALID_TRANSITION");
    }

    private static StockTransfer CreateTransfer()
        => StockTransfer.Create(
            Guid.NewGuid(),
            Now,
            ProductId,
            SourceWarehouseId,
            SourceLocationId,
            TargetWarehouseId,
            TargetLocationId,
            5,
            Guid.Parse("40000000-0000-0000-0000-000000000106"),
            "Packaging",
            PositiveQuantity.Create(10_000m, 6),
            "{\"name\":\"Koli\",\"quantityInBaseUom\":2000}");
}
