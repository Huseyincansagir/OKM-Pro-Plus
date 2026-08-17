namespace FactoryErp.Application.Shipping;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public sealed record DispatchStopInput(
    Guid RouteStopId,
    int SequenceNo);

public sealed record PrepareDispatchRunCommand(
    Guid ShipmentId,
    Guid LoadPlanId,
    Guid RoutePlanId,
    Guid VehicleId,
    Guid DriverId,
    DateTimeOffset? PlannedDepartureAt,
    IReadOnlyCollection<DispatchStopInput> Stops,
    long ExpectedLoadPlanRowVersion,
    long ExpectedShipmentRowVersion,
    long ExpectedRoutePlanRowVersion,
    Guid ActorId,
    string IdempotencyKey,
    string CorrelationId);

public sealed record ConfirmDispatchCommand(
    Guid DispatchRunId,
    long ExpectedDispatchRunRowVersion,
    Guid ActorId,
    string IdempotencyKey,
    string CorrelationId);

public sealed record DepartDispatchRunCommand(
    Guid DispatchRunId,
    DateTimeOffset OccurredAt,
    string? LocationText,
    decimal? Latitude,
    decimal? Longitude,
    long ExpectedDispatchRunRowVersion,
    Guid ActorId,
    string IdempotencyKey,
    string CorrelationId);

public sealed record ArriveAtStopCommand(
    Guid DispatchRunId,
    Guid RouteStopId,
    DateTimeOffset OccurredAt,
    string? LocationText,
    decimal? Latitude,
    decimal? Longitude,
    long ExpectedDispatchRunRowVersion,
    Guid ActorId,
    string IdempotencyKey,
    string CorrelationId);

public sealed record DepartStopCommand(
    Guid DispatchRunId,
    Guid RouteStopId,
    DateTimeOffset OccurredAt,
    string? LocationText,
    decimal? Latitude,
    decimal? Longitude,
    long ExpectedDispatchRunRowVersion,
    Guid ActorId,
    string IdempotencyKey,
    string CorrelationId);

public sealed record SkipStopCommand(
    Guid DispatchRunId,
    Guid RouteStopId,
    DateTimeOffset OccurredAt,
    string Reason,
    long ExpectedDispatchRunRowVersion,
    Guid ActorId,
    string IdempotencyKey,
    string CorrelationId);

public sealed record CompleteRouteCommand(
    Guid DispatchRunId,
    DateTimeOffset OccurredAt,
    long ExpectedDispatchRunRowVersion,
    Guid ActorId,
    string IdempotencyKey,
    string CorrelationId);

public sealed record CancelDispatchRunCommand(
    Guid DispatchRunId,
    DateTimeOffset OccurredAt,
    string Reason,
    long ExpectedDispatchRunRowVersion,
    Guid ActorId,
    string IdempotencyKey,
    string CorrelationId);

public sealed record DispatchRunStopDto(
    Guid RouteStopId,
    int SequenceNo,
    string Status);

public sealed record RouteExecutionEventDto(
    Guid Id,
    Guid DispatchRunId,
    Guid RoutePlanId,
    Guid? RouteStopId,
    string EventType,
    long SequenceNo,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string? LocationText,
    decimal? Latitude,
    decimal? Longitude,
    string? Reason,
    string IdempotencyKey,
    string CorrelationId);

public sealed record DispatchRunDto(
    Guid Id,
    Guid ShipmentId,
    Guid LoadPlanId,
    Guid RoutePlanId,
    Guid VehicleId,
    Guid DriverId,
    string Status,
    DateTimeOffset? PlannedDepartureAt,
    DateTimeOffset? ActualDepartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt,
    Guid? DispatchedBy,
    Guid? CompletedBy,
    Guid? CancelledBy,
    string? ExceptionReason,
    IReadOnlyCollection<DispatchRunStopDto> Stops,
    IReadOnlyCollection<RouteExecutionEventDto> Events,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long RowVersion);

public interface IDispatchRunCommandHandler
    : ICommandHandler<PrepareDispatchRunCommand, DispatchRunDto>,
      ICommandHandler<ConfirmDispatchCommand, DispatchRunDto>,
      ICommandHandler<DepartDispatchRunCommand, DispatchRunDto>,
      ICommandHandler<ArriveAtStopCommand, DispatchRunDto>,
      ICommandHandler<DepartStopCommand, DispatchRunDto>,
      ICommandHandler<SkipStopCommand, DispatchRunDto>,
      ICommandHandler<CompleteRouteCommand, DispatchRunDto>,
      ICommandHandler<CancelDispatchRunCommand, DispatchRunDto>
{
}
