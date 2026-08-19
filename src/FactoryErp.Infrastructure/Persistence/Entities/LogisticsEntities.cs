namespace FactoryErp.Infrastructure.Persistence.Entities;

public sealed class ShipmentRecord
{
    public Guid Id { get; set; }
    public Guid DeliveryNoteId { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = "Preparing";
    public long RowVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public DeliveryNoteRecord DeliveryNote { get; set; } = null!;
    public ICollection<ShipmentItemRecord> Items { get; } = new List<ShipmentItemRecord>();
    public ICollection<RoutePlanRecord> RoutePlans { get; } = new List<RoutePlanRecord>();
}

public sealed class ShipmentItemRecord
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid DeliveryNoteItemId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityBase { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";

    public ShipmentRecord Shipment { get; set; } = null!;
    public DeliveryNoteItemRecord DeliveryNoteItem { get; set; } = null!;
}

public sealed class VehicleTypeRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<VehicleCapacityRecord> Capacities { get; } = new List<VehicleCapacityRecord>();
    public ICollection<VehicleRecord> Vehicles { get; } = new List<VehicleRecord>();
}

public sealed class VehicleCapacityRecord
{
    public Guid Id { get; set; }
    public Guid VehicleTypeId { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public decimal MaxGrossWeight { get; set; }
    public decimal TareWeight { get; set; }
    public decimal MaxUsableVolume { get; set; }
    public int MaxPalletCount { get; set; }
    public decimal MaxLoadHeight { get; set; }
    public string CapacityPolicySnapshot { get; set; } = "{}";
    public long RowVersion { get; set; }

    public VehicleTypeRecord VehicleType { get; set; } = null!;
    public ICollection<VehicleCapacityPalletTypeRecord> PalletTypes { get; } = new List<VehicleCapacityPalletTypeRecord>();
    public ICollection<VehicleCapacityZoneRecord> Zones { get; } = new List<VehicleCapacityZoneRecord>();
}

public sealed class VehicleRecord
{
    public Guid Id { get; set; }
    public Guid VehicleTypeId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
    public DateTimeOffset? MaintenanceUntil { get; set; }
    public Guid? CurrentRoutePlanId { get; set; }
    public string? LastKnownLocationText { get; set; }
    public DateTimeOffset LastStatusAt { get; set; }
    public long RowVersion { get; set; }

    public VehicleTypeRecord VehicleType { get; set; } = null!;
    public ICollection<RoutePlanRecord> RoutePlans { get; } = new List<RoutePlanRecord>();
}

public sealed class DriverRecord
{
    public Guid Id { get; set; }
    public Guid? EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateOnly LicenseExpiry { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public long RowVersion { get; set; }

    public ICollection<RoutePlanRecord> RoutePlans { get; } = new List<RoutePlanRecord>();
}

public sealed class RoutePlanRecord
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public string Status { get; set; } = "Draft";
    public int Version { get; set; } = 1;
    public Guid? ReplannedFromId { get; set; }
    public DateTimeOffset? PlannedStartAt { get; set; }
    public DateTimeOffset? PlannedEndAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public ShipmentRecord Shipment { get; set; } = null!;
    public VehicleRecord? Vehicle { get; set; }
    public DriverRecord? Driver { get; set; }
    public RoutePlanRecord? ReplannedFrom { get; set; }
    public ICollection<RoutePlanRecord> Replans { get; } = new List<RoutePlanRecord>();
    public ICollection<RouteStopRecord> Stops { get; } = new List<RouteStopRecord>();
}

public sealed class RouteStopRecord
{
    public Guid Id { get; set; }
    public Guid RoutePlanId { get; set; }
    public int SequenceNo { get; set; }
    public Guid CustomerId { get; set; }
    public Guid AddressId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? PlannedArrivalAt { get; set; }
    public DateTimeOffset? ActualArrivalAt { get; set; }
    public DateTimeOffset? ActualDepartureAt { get; set; }
    public DateTimeOffset? SkippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? ProofRecipient { get; set; }
    public string? ProofNote { get; set; }
    public string? ExceptionReason { get; set; }
    public long RowVersion { get; set; }

    public RoutePlanRecord RoutePlan { get; set; } = null!;
    public CustomerRecord Customer { get; set; } = null!;
    public CustomerAddressRecord Address { get; set; } = null!;
}


public sealed class ShipmentPackageRecord
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid ShipmentItemId { get; set; }
    public Guid? PackagingId { get; set; }
    public Guid? RouteStopId { get; set; }
    public string PackageType { get; set; } = string.Empty;
    public decimal PackageCount { get; set; }
    public decimal QuantityBasePerPackage { get; set; }
    public decimal QuantityBase { get; set; }
    public decimal? EnteredQuantity { get; set; }
    public string? PackageCode { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";
    public string PhysicalSnapshot { get; set; } = "{}";
    public bool SplitAllowed { get; set; }
    public string Status { get; set; } = "Available";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public ShipmentRecord Shipment { get; set; } = null!;
    public ShipmentItemRecord ShipmentItem { get; set; } = null!;
    public ProductPackagingRecord? Packaging { get; set; }
    public RouteStopRecord? RouteStop { get; set; }
}


public sealed class LoadPlanRecord
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid RoutePlanId { get; set; }
    public int RoutePlanVersion { get; set; }
    public int Version { get; set; }
    public Guid? ReplannedFromId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? VehicleCapacityId { get; set; }
    public string Status { get; set; } = "Draft";
    public string FeasibilityStatus { get; set; } = "Infeasible";
    public string? AlgorithmName { get; set; }
    public string? AlgorithmVersion { get; set; }
    public string? ParameterSet { get; set; }
    public string? InputSnapshotHash { get; set; }
    public string? CapacitySnapshot { get; set; }
    public string? UtilizationSnapshot { get; set; }
    public string ValidationSummary { get; set; } = "{}";
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? LockedBy { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public ShipmentRecord Shipment { get; set; } = null!;
    public RoutePlanRecord RoutePlan { get; set; } = null!;
    public LoadPlanRecord? ReplannedFrom { get; set; }
    public ICollection<LoadPlanRecord> Replans { get; } = new List<LoadPlanRecord>();
    public ICollection<LoadUnitRecord> LoadUnits { get; } = new List<LoadUnitRecord>();
}

public sealed class LoadUnitRecord
{
    public Guid Id { get; set; }
    public Guid LoadPlanId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public Guid? PalletTypeId { get; set; }
    public bool IsMixed { get; set; }
    public decimal LengthMm { get; set; }
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public decimal TareWeightKg { get; set; }
    public decimal GrossWeightKg { get; set; }
    public decimal VolumeM3 { get; set; }
    public int? MaxStackCount { get; set; }
    public string? PlacementZone { get; set; }
    public int UnloadingPriority { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public LoadPlanRecord LoadPlan { get; set; } = null!;
    public PalletTypeRecord? PalletType { get; set; }
    public ICollection<LoadUnitItemRecord> Items { get; } = new List<LoadUnitItemRecord>();
}

public sealed class LoadUnitItemRecord
{
    public Guid Id { get; set; }
    public Guid LoadUnitId { get; set; }
    public Guid ShipmentPackageId { get; set; }
    public Guid ShipmentItemId { get; set; }
    public decimal QuantityBase { get; set; }
    public decimal GrossWeightKg { get; set; }
    public decimal VolumeM3 { get; set; }
    public string AllocationSnapshot { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public long RowVersion { get; set; }

    public LoadUnitRecord LoadUnit { get; set; } = null!;
    public ShipmentPackageRecord ShipmentPackage { get; set; } = null!;
    public ShipmentItemRecord ShipmentItem { get; set; } = null!;
    public ICollection<LoadUnitStopAllocationRecord> StopAllocations { get; } = new List<LoadUnitStopAllocationRecord>();
}

public sealed class LoadUnitStopAllocationRecord
{
    public Guid Id { get; set; }
    public Guid LoadUnitItemId { get; set; }
    public Guid RouteStopId { get; set; }
    public decimal QuantityBase { get; set; }
    public int SequenceNo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public LoadUnitItemRecord LoadUnitItem { get; set; } = null!;
    public RouteStopRecord RouteStop { get; set; } = null!;
}


public sealed class VehicleFitEvaluationRecord
{
    public Guid Id { get; set; }
    public Guid LoadPlanId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid? VehicleCapacityId { get; set; }
    public string CandidateStatus { get; set; } = "Rejected";
    public string? RejectionCode { get; set; }
    public string? ReasonText { get; set; }
    public decimal? WeightRatio { get; set; }
    public decimal? VolumeRatio { get; set; }
    public decimal? PalletRatio { get; set; }
    public decimal? FloorAreaRatio { get; set; }
    public decimal? HeightRatio { get; set; }
    public string DoorCheckStatus { get; set; } = "NotChecked";
    public string DimensionCheckStatus { get; set; } = "NotChecked";
    public string StackingCheckStatus { get; set; } = "NotChecked";
    public string AxleCheckStatus { get; set; } = "NotChecked";
    public string StopAccessStatus { get; set; } = "NotChecked";
    public decimal? FitScore { get; set; }
    public string AlgorithmVersion { get; set; } = string.Empty;
    public string InputSnapshotHash { get; set; } = string.Empty;
    public string? CapacitySnapshot { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }

    public LoadPlanRecord LoadPlan { get; set; } = null!;
    public VehicleRecord Vehicle { get; set; } = null!;
    public VehicleCapacityRecord? VehicleCapacity { get; set; }
}


public sealed class LoadPlanValidationResultRecord
{
    public Guid Id { get; set; }
    public Guid LoadPlanId { get; set; }
    public string ValidationKey { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string ResolutionStatus { get; set; } = "Open";
    public Guid? ResolvedBy { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public LoadPlanRecord LoadPlan { get; set; } = null!;
    public UserRecord? Resolver { get; set; }
}

public sealed class LoadPlanManualChangeRecord
{
    public Guid Id { get; set; }
    public Guid LoadPlanId { get; set; }
    public Guid ActorUserId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public LoadPlanRecord LoadPlan { get; set; } = null!;
    public UserRecord ActorUser { get; set; } = null!;
}


public sealed class LoadVerificationSessionRecord
{
    public Guid Id { get; set; }
    public Guid LoadPlanId { get; set; }
    public Guid ShipmentId { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid StartedBy { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public Guid? CompletedBy { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public LoadPlanRecord LoadPlan { get; set; } = null!;
    public ShipmentRecord Shipment { get; set; } = null!;
    public UserRecord StartedByUser { get; set; } = null!;
    public UserRecord? CompletedByUser { get; set; }
    public ICollection<LoadVerificationScanRecord> Scans { get; } = new List<LoadVerificationScanRecord>();
}

public sealed class LoadVerificationScanRecord
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid LoadPlanId { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid? ShipmentPackageId { get; set; }
    public Guid? ExpectedLoadUnitId { get; set; }
    public Guid? ActualLoadUnitId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Status { get; set; } = "Accepted";
    public string ScanMode { get; set; } = "Package";
    public decimal QuantityBase { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonText { get; set; }
    public Guid ScannedBy { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public long RowVersion { get; set; }

    public LoadVerificationSessionRecord Session { get; set; } = null!;
    public LoadPlanRecord LoadPlan { get; set; } = null!;
    public ShipmentRecord Shipment { get; set; } = null!;
    public ShipmentPackageRecord? ShipmentPackage { get; set; }
    public LoadUnitRecord? ExpectedLoadUnit { get; set; }
    public LoadUnitRecord? ActualLoadUnit { get; set; }
    public UserRecord ScannedByUser { get; set; } = null!;
}


public sealed class DispatchRunRecord
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid LoadPlanId { get; set; }
    public Guid RoutePlanId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DriverId { get; set; }
    public string Status { get; set; } = "Prepared";
    public DateTimeOffset? PlannedDepartureAt { get; set; }
    public DateTimeOffset? ActualDepartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? DispatchedBy { get; set; }
    public Guid? CompletedBy { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? ExceptionReason { get; set; }
    public long RowVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ShipmentRecord Shipment { get; set; } = null!;
    public LoadPlanRecord LoadPlan { get; set; } = null!;
    public RoutePlanRecord RoutePlan { get; set; } = null!;
    public VehicleRecord Vehicle { get; set; } = null!;
    public DriverRecord Driver { get; set; } = null!;
    public ICollection<RouteExecutionEventRecord> Events { get; } = new List<RouteExecutionEventRecord>();
}

public sealed class RouteExecutionEventRecord
{
    public Guid Id { get; set; }
    public Guid DispatchRunId { get; set; }
    public Guid RoutePlanId { get; set; }
    public Guid? RouteStopId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long SequenceNo { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? LocationText { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Reason { get; set; }
    public Guid ActorId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string PayloadSnapshot { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }

    public DispatchRunRecord DispatchRun { get; set; } = null!;
    public RoutePlanRecord RoutePlan { get; set; } = null!;
    public RouteStopRecord? RouteStop { get; set; }
}
