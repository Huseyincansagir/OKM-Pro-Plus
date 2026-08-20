using FactoryErp.Application.Warehouse;
using FluentAssertions;

namespace FactoryErp.Infrastructure.UnitTests.Warehouse;

public sealed class StockMovementEffectTests
{
    [Fact]
    public void Known_movement_types_map_to_in_or_out_without_signing_quantity()
    {
        StockMovementEffects.FromMovementType("ProductionIn").Should().Be(StockMovementEffects.In);
        StockMovementEffects.FromMovementType("WarehouseTransferIn").Should().Be(StockMovementEffects.In);
        StockMovementEffects.FromMovementType("CountIn").Should().Be(StockMovementEffects.In);
        StockMovementEffects.FromMovementType("WarehouseTransferOut").Should().Be(StockMovementEffects.Out);
        StockMovementEffects.FromMovementType("DeliveryIssue").Should().Be(StockMovementEffects.Out);
        StockMovementEffects.FromMovementType("CountOut").Should().Be(StockMovementEffects.Out);
        StockMovementEffects.FromMovementType("FutureAdjust").Should().Be(StockMovementEffects.Unknown);
        StockMovementEffects.FromMovementType("").Should().Be(StockMovementEffects.Unknown);
    }
}
