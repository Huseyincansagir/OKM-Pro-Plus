namespace FactoryErp.Infrastructure.Persistence.Entities;

public sealed class EmployeeRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = "Active";
    public DateOnly? HiredOn { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class StockCountRecord
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Guid LocationId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid CreatedBy { get; set; }

    public ICollection<StockCountItemRecord> Items { get; } = new List<StockCountItemRecord>();
}

public sealed class StockCountItemRecord
{
    public Guid Id { get; set; }
    public Guid StockCountId { get; set; }
    public Guid ProductId { get; set; }
    public decimal CountedQtyBase { get; set; }
    public decimal SystemOnHandQtyBase { get; set; }
    public decimal VarianceQtyBase { get; set; }

    public StockCountRecord StockCount { get; set; } = null!;
}
