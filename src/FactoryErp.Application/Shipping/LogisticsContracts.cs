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
