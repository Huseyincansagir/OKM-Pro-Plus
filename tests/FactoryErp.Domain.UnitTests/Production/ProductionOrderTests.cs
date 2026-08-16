using FactoryErp.Domain.Common;
using FactoryErp.Domain.Production;
using FactoryErp.Domain.Shared;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Production;

public sealed class ProductionOrderTests
{
    private static readonly Guid ProductId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid WarehouseId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_accepts_positive_planned_quantity()
    {
        var order = CreateOrder(2_000m);

        order.Status.Should().Be(ProductionOrderStatus.Planned);
        order.PlannedQuantity.BaseValue.Should().Be(2_000m);
        order.CompletedQuantity.BaseValue.Should().Be(0m);
        order.RemainingQuantity.BaseValue.Should().Be(2_000m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_zero_or_negative_planned_quantity(decimal quantity)
    {
        var action = () => ProductionOrder.Create(
            Guid.NewGuid(),
            ProductId,
            WarehouseId,
            PositiveQuantity.Create(quantity, 6),
            Now);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("QUANTITY_MUST_BE_POSITIVE");
    }

    [Fact]
    public void Lifecycle_accepts_exact_boundary_and_completes()
    {
        var order = CreateOrder(2_000m);
        order.Release(Now);
        order.Start(Now);
        order.RecordProduction(PositiveQuantity.Create(2_000m, 6), Now);
        order.Complete(Now);

        order.Status.Should().Be(ProductionOrderStatus.Completed);
        order.CompletedQuantity.BaseValue.Should().Be(2_000m);
        order.RemainingQuantity.BaseValue.Should().Be(0m);
    }

    [Fact]
    public void Record_rejects_over_allocation()
    {
        var order = CreateOrder(2_000m);
        order.Release(Now);
        order.Start(Now);
        order.RecordProduction(PositiveQuantity.Create(1_500m, 6), Now);

        var action = () => order.RecordProduction(PositiveQuantity.Create(501m, 6), Now);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("PRODUCTION_QUANTITY_EXCEEDS_PLAN");
        order.CompletedQuantity.BaseValue.Should().Be(1_500m);
    }

    [Fact]
    public void Record_rejects_when_order_is_not_in_progress()
    {
        var order = CreateOrder(2_000m);

        var action = () => order.RecordProduction(PositiveQuantity.Create(1m, 6), Now);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("PRODUCTION_RECORD_INVALID_STATE");
    }

    [Fact]
    public void Release_rejects_invalid_state_transition()
    {
        var order = CreateOrder(2_000m);
        order.Release(Now);

        var action = () => order.Release(Now);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("PRODUCTION_ORDER_NOT_RELEASEABLE");
    }

    [Fact]
    public void Complete_rejects_without_a_production_record()
    {
        var order = CreateOrder(2_000m);
        order.Release(Now);
        order.Start(Now);

        var action = () => order.Complete(Now);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("PRODUCTION_COMPLETION_REQUIRES_RECORD");
    }

    [Fact]
    public void Cancel_rejects_an_in_progress_order()
    {
        var order = CreateOrder(2_000m);
        order.Release(Now);
        order.Start(Now);

        var action = () => order.Cancel(Now);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("PRODUCTION_ORDER_NOT_CANCELLABLE");
    }

    [Fact]
    public void Rehydrate_rejects_completed_quantity_above_plan()
    {
        var action = () => ProductionOrder.Rehydrate(
            Guid.NewGuid(),
            ProductId,
            WarehouseId,
            PositiveQuantity.Create(2_000m, 6),
            NonNegativeQuantity.Create(2_001m, 6),
            ProductionOrderStatus.InProgress,
            Now);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("PRODUCTION_INVARIANT_VIOLATION");
    }

    private static ProductionOrder CreateOrder(decimal plannedQuantity)
        => ProductionOrder.Create(
            Guid.NewGuid(),
            ProductId,
            WarehouseId,
            PositiveQuantity.Create(plannedQuantity, 6),
            Now);
}
