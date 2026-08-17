namespace FactoryErp.Application.Shipping;

public sealed record CreateVehicleTypeRequest(string Code, string Name);

public sealed record CreateVehicleRequest(
    Guid VehicleTypeId,
    string PlateNumber,
    DateTimeOffset? MaintenanceUntil,
    string? LastKnownLocationText);

public sealed record CreateVehicleCapacityRequest(
    Guid VehicleTypeId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    decimal MaxGrossWeight,
    decimal TareWeight,
    decimal MaxUsableVolume,
    int MaxPalletCount,
    decimal MaxLoadHeight,
    string CapacityPolicySnapshot);

public sealed record ChangeVehicleStatusRequest(string Status, string? Reason);

public sealed record CreateDriverRequest(
    Guid? EmployeeId,
    string FullName,
    string? Phone,
    string LicenseNumber,
    DateOnly LicenseExpiry);

public sealed record ChangeDriverStatusRequest(string Status, string? Reason);

public sealed record CreateShipmentRequest(
    Guid DeliveryNoteId,
    long ExpectedDeliveryNoteRowVersion);

public sealed record CreateRoutePlanRequest(
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt,
    long ExpectedShipmentRowVersion);

public sealed record RouteStopInput(
    int SequenceNo,
    Guid CustomerId,
    Guid AddressId,
    DateTimeOffset? PlannedArrivalAt);

public sealed record ReplaceRouteStopsRequest(
    IReadOnlyCollection<RouteStopInput> Stops);

public sealed record AssignRouteResourcesRequest(
    Guid VehicleId,
    Guid DriverId);

public sealed record RouteConfirmationRequest(bool Confirmation);

public sealed record ReplanRouteRequest(
    string Reason,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt);

public sealed record VehicleTypeDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);

public sealed record VehicleCapacityDto(
    Guid Id,
    Guid VehicleTypeId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    decimal MaxGrossWeight,
    decimal TareWeight,
    decimal MaxPayloadWeight,
    decimal MaxUsableVolume,
    int MaxPalletCount,
    decimal MaxLoadHeight,
    long RowVersion);

public sealed record VehicleDto(
    Guid Id,
    Guid VehicleTypeId,
    string PlateNumber,
    string Status,
    DateTimeOffset? MaintenanceUntil,
    Guid? CurrentRoutePlanId,
    string? LastKnownLocationText,
    DateTimeOffset LastStatusAt,
    long RowVersion);

public sealed record DriverDto(
    Guid Id,
    Guid? EmployeeId,
    string FullName,
    string? Phone,
    string LicenseNumber,
    DateOnly LicenseExpiry,
    string Status,
    bool IsActive,
    long RowVersion);

public sealed record ShipmentDto(
    Guid Id,
    Guid DeliveryNoteId,
    Guid CustomerId,
    string Status,
    IReadOnlyCollection<ShipmentItemDto> Items,
    long RowVersion,
    DateTimeOffset CreatedAt);

public sealed record ShipmentItemDto(
    Guid Id,
    Guid DeliveryNoteItemId,
    Guid ProductId,
    decimal QuantityBase,
    string PackagingSnapshot);

public sealed record RouteStopDto(
    Guid Id,
    int SequenceNo,
    Guid CustomerId,
    Guid AddressId,
    string Status,
    DateTimeOffset? PlannedArrivalAt,
    long RowVersion);

public sealed record RoutePlanDto(
    Guid Id,
    Guid ShipmentId,
    Guid? VehicleId,
    Guid? DriverId,
    string Status,
    int Version,
    Guid? ReplannedFromId,
    DateTimeOffset? PlannedStartAt,
    DateTimeOffset? PlannedEndAt,
    IReadOnlyCollection<RouteStopDto> Stops,
    long RowVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface ILogisticsCommandService
{
    Task<VehicleTypeDto> CreateVehicleTypeAsync(
        CreateVehicleTypeRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<VehicleTypeDto?> GetVehicleTypeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VehicleDto> CreateVehicleAsync(
        CreateVehicleRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<VehicleCapacityDto> CreateVehicleCapacityAsync(
        CreateVehicleCapacityRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<VehicleDto?> GetVehicleAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VehicleDto?> ChangeVehicleStatusAsync(
        Guid id,
        ChangeVehicleStatusRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<DriverDto> CreateDriverAsync(
        CreateDriverRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<DriverDto?> GetDriverAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DriverDto?> ChangeDriverStatusAsync(
        Guid id,
        ChangeDriverStatusRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<ShipmentDto> CreateShipmentAsync(
        CreateShipmentRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<ShipmentDto?> GetShipmentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoutePlanDto> CreateRoutePlanAsync(
        Guid shipmentId,
        CreateRoutePlanRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<RoutePlanDto?> GetRoutePlanAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoutePlanDto?> ReplaceStopsAsync(
        Guid routePlanId,
        ReplaceRouteStopsRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<RoutePlanDto?> AssignResourcesAsync(
        Guid routePlanId,
        AssignRouteResourcesRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<RoutePlanDto?> PlanRouteAsync(
        Guid routePlanId,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<RoutePlanDto?> LockRouteAsync(
        Guid routePlanId,
        RouteConfirmationRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<RoutePlanDto?> ReplanRouteAsync(
        Guid routePlanId,
        ReplanRouteRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);
}


public sealed record CreateShipmentPackageRequest(
    Guid ShipmentItemId,
    Guid? PackagingId,
    Guid? RouteStopId,
    string PackageType,
    decimal PackageCount,
    decimal QuantityBasePerPackage,
    decimal? EnteredQuantity,
    string? PackageCode,
    bool SplitAllowed);

public sealed record ShipmentPackageDto(
    Guid Id,
    Guid ShipmentId,
    Guid ShipmentItemId,
    Guid? PackagingId,
    Guid? RouteStopId,
    string PackageType,
    decimal PackageCount,
    decimal QuantityBasePerPackage,
    decimal QuantityBase,
    decimal? EnteredQuantity,
    string? PackageCode,
    string PackagingSnapshot,
    string PhysicalSnapshot,
    bool SplitAllowed,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long RowVersion);

public interface IShipmentPackageCommandService
{
    Task<ShipmentPackageDto> CreateShipmentPackageAsync(
        Guid shipmentId,
        CreateShipmentPackageRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ShipmentPackageDto>> GetShipmentPackagesAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    Task<ShipmentPackageDto?> GetShipmentPackageAsync(
        Guid packageId,
        CancellationToken cancellationToken = default);
}


public sealed record CreateLoadPlanRequest(
    Guid RoutePlanId,
    int ExpectedRoutePlanVersion,
    long ExpectedShipmentRowVersion,
    IReadOnlyCollection<CreateLoadUnitRequest> LoadUnits);

public sealed record CreateLoadUnitRequest(
    string UnitCode,
    string UnitType,
    Guid? PalletTypeId,
    bool IsMixed,
    decimal LengthMm,
    decimal WidthMm,
    decimal HeightMm,
    decimal TareWeightKg,
    decimal GrossWeightKg,
    decimal VolumeM3,
    int? MaxStackCount,
    string? PlacementZone,
    int UnloadingPriority,
    IReadOnlyCollection<CreateLoadUnitItemRequest> Items);

public sealed record CreateLoadUnitItemRequest(
    Guid ShipmentPackageId,
    Guid ShipmentItemId,
    decimal QuantityBase,
    IReadOnlyCollection<CreateLoadUnitStopAllocationRequest> StopAllocations);

public sealed record CreateLoadUnitStopAllocationRequest(
    Guid RouteStopId,
    decimal QuantityBase,
    int SequenceNo);

public sealed record LoadPlanDto(
    Guid Id,
    Guid ShipmentId,
    Guid RoutePlanId,
    int RoutePlanVersion,
    int Version,
    Guid? ReplannedFromId,
    Guid? VehicleId,
    Guid? VehicleCapacityId,
    string Status,
    string FeasibilityStatus,
    string? AlgorithmName,
    string? AlgorithmVersion,
    string? ParameterSet,
    string? InputSnapshotHash,
    string? CapacitySnapshot,
    string? UtilizationSnapshot,
    string ValidationSummary,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    Guid? LockedBy,
    DateTimeOffset? LockedAt,
    IReadOnlyCollection<LoadUnitDto> LoadUnits,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long RowVersion);

public sealed record LoadUnitDto(
    Guid Id,
    Guid LoadPlanId,
    string UnitCode,
    string UnitType,
    Guid? PalletTypeId,
    bool IsMixed,
    decimal LengthMm,
    decimal WidthMm,
    decimal HeightMm,
    decimal TareWeightKg,
    decimal GrossWeightKg,
    decimal VolumeM3,
    int? MaxStackCount,
    string? PlacementZone,
    int UnloadingPriority,
    string Status,
    IReadOnlyCollection<LoadUnitItemDto> Items,
    DateTimeOffset CreatedAt,
    long RowVersion);

public sealed record LoadUnitItemDto(
    Guid Id,
    Guid LoadUnitId,
    Guid ShipmentPackageId,
    Guid ShipmentItemId,
    decimal QuantityBase,
    decimal GrossWeightKg,
    decimal VolumeM3,
    string AllocationSnapshot,
    IReadOnlyCollection<LoadUnitStopAllocationDto> StopAllocations,
    DateTimeOffset CreatedAt,
    long RowVersion);

public sealed record LoadUnitStopAllocationDto(
    Guid Id,
    Guid LoadUnitItemId,
    Guid RouteStopId,
    decimal QuantityBase,
    int SequenceNo,
    DateTimeOffset CreatedAt);

public interface ILoadPlanValidationCommandService
{
    Task<LoadPlanValidationDto> ValidateLoadPlanAsync(
        Guid loadPlanId,
        ValidateLoadPlanRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<LoadPlanValidationResultDto> ResolveValidationResultAsync(
        Guid loadPlanId,
        Guid validationResultId,
        ResolveLoadPlanValidationRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<LoadPlanDto> CreateManualChangeAsync(
        Guid loadPlanId,
        CreateLoadPlanManualChangeRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<LoadPlanDto> LockLoadPlanAsync(
        Guid loadPlanId,
        LockLoadPlanRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LoadPlanValidationResultDto>> GetValidationResultsAsync(
        Guid loadPlanId,
        CancellationToken cancellationToken = default);
}

public interface ILoadPlanCommandService : ILoadPlanValidationCommandService
{
    Task<LoadPlanDto> CreateLoadPlanAsync(
        Guid shipmentId,
        CreateLoadPlanRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<LoadPlanDto?> GetLoadPlanAsync(
        Guid loadPlanId,
        CancellationToken cancellationToken = default);
}


public sealed record EvaluateVehicleFitRequest(
    Guid LoadPlanId,
    long ExpectedLoadPlanRowVersion,
    IReadOnlyCollection<Guid>? VehicleIds,
    string? AlgorithmVersion,
    string? ParameterSet);

public sealed record VehicleFitEvaluationDto(
    Guid Id,
    Guid LoadPlanId,
    Guid VehicleId,
    Guid? VehicleCapacityId,
    string CandidateStatus,
    string? RejectionCode,
    string? ReasonText,
    decimal? WeightRatio,
    decimal? VolumeRatio,
    decimal? PalletRatio,
    decimal? FloorAreaRatio,
    decimal? HeightRatio,
    string DoorCheckStatus,
    string DimensionCheckStatus,
    string StackingCheckStatus,
    string AxleCheckStatus,
    string StopAccessStatus,
    decimal? FitScore,
    string AlgorithmVersion,
    string InputSnapshotHash,
    string? CapacitySnapshot,
    DateTimeOffset EvaluatedAt);

public sealed record VehicleFitEvaluationBatchDto(
    Guid LoadPlanId,
    Guid ShipmentId,
    string AlgorithmName,
    string AlgorithmVersion,
    string ParameterSet,
    string InputSnapshotHash,
    IReadOnlyCollection<VehicleFitEvaluationDto> Evaluations,
    IReadOnlyCollection<Guid> MissingPhysicalProfilePackageIds);

public interface IVehicleFitCommandService
{
    Task<VehicleFitEvaluationBatchDto> EvaluateVehicleFitAsync(
        Guid shipmentId,
        EvaluateVehicleFitRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VehicleFitEvaluationDto>> GetVehicleFitCandidatesAsync(
        Guid shipmentId,
        Guid loadPlanId,
        CancellationToken cancellationToken = default);
}


public sealed record ValidateLoadPlanRequest;

public sealed record LoadPlanValidationResultDto(
    Guid Id,
    Guid LoadPlanId,
    string ValidationKey,
    string Severity,
    string Code,
    string Message,
    string? EntityType,
    Guid? EntityId,
    string ResolutionStatus,
    Guid? ResolvedBy,
    DateTimeOffset? ResolvedAt,
    string? ResolutionReason,
    DateTimeOffset CreatedAt);

public sealed record LoadPlanValidationDto(
    LoadPlanDto LoadPlan,
    IReadOnlyCollection<LoadPlanValidationResultDto> Results);

public sealed record ResolveLoadPlanValidationRequest(
    string ResolutionStatus,
    string Reason);

public sealed record CreateLoadPlanManualChangeRequest(
    string ChangeType,
    string EntityType,
    Guid EntityId,
    string BeforeJson,
    string AfterJson,
    string Reason);

public sealed record LockLoadPlanRequest(
    bool Approval,
    IReadOnlyCollection<WarningResolutionInput>? WarningResolutions);

public sealed record WarningResolutionInput(
    Guid ValidationResultId,
    string Action,
    string Reason);


public sealed record StartLoadVerificationRequest;

public sealed record ScanLoadVerificationRequest(
    string Barcode,
    Guid? ExpectedLoadUnitId,
    string ScanMode);

public sealed record CompleteLoadVerificationRequest;

public sealed record CloseLoadVerificationDiscrepancyRequest(
    string Reason);

public sealed record LoadVerificationScanDto(
    Guid Id,
    Guid SessionId,
    Guid LoadPlanId,
    Guid ShipmentId,
    Guid? ShipmentPackageId,
    Guid? ExpectedLoadUnitId,
    Guid? ActualLoadUnitId,
    string Barcode,
    string Status,
    string ScanMode,
    decimal QuantityBase,
    string? ReasonCode,
    string? ReasonText,
    Guid ScannedBy,
    DateTimeOffset ScannedAt,
    string IdempotencyKey,
    string CorrelationId,
    long RowVersion);

public sealed record LoadVerificationSessionDto(
    Guid Id,
    Guid LoadPlanId,
    Guid ShipmentId,
    string Status,
    Guid StartedBy,
    DateTimeOffset StartedAt,
    Guid? CompletedBy,
    DateTimeOffset? CompletedAt,
    string? CompletionReason,
    IReadOnlyCollection<LoadVerificationScanDto> Scans,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long RowVersion);

public interface ILoadVerificationCommandService
{
    Task<LoadVerificationSessionDto> StartSessionAsync(
        Guid loadPlanId,
        StartLoadVerificationRequest request,
        long expectedLoadPlanRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<LoadVerificationSessionDto?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<LoadVerificationScanDto> ScanAsync(
        Guid sessionId,
        ScanLoadVerificationRequest request,
        long expectedSessionRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<LoadVerificationSessionDto> CompleteAsync(
        Guid sessionId,
        CompleteLoadVerificationRequest request,
        long expectedSessionRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<LoadVerificationSessionDto> CloseDiscrepancyAsync(
        Guid sessionId,
        CloseLoadVerificationDiscrepancyRequest request,
        long expectedSessionRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);
}
