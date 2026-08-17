using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Shipping;

public enum DispatchRunStatus
{
    Prepared,
    Dispatched,
    InTransit,
    Completed,
    Cancelled,
}

public enum RouteExecutionEventType
{
    Departed,
    ArrivedAtStop,
    DepartedStop,
    SkippedStop,
    RouteCompleted,
    Cancelled,
}

public enum RouteStopExecutionStatus
{
    Pending,
    Arrived,
    Departed,
    Skipped,
}

public sealed record DispatchRunStop(
    Guid RouteStopId,
    int SequenceNo,
    RouteStopExecutionStatus Status = RouteStopExecutionStatus.Pending)
{
    public DispatchRunStop WithStatus(RouteStopExecutionStatus status) => this with { Status = status };
}

public sealed record RouteExecutionEvent(
    Guid Id,
    Guid DispatchRunId,
    Guid RoutePlanId,
    Guid? RouteStopId,
    RouteExecutionEventType EventType,
    long SequenceNo,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string? LocationText,
    decimal? Latitude,
    decimal? Longitude,
    string? Reason,
    string IdempotencyKey,
    string CorrelationId);

public sealed class DispatchRun : AggregateRoot
{
    private readonly List<DispatchRunStop> _stops;
    private readonly List<RouteExecutionEvent> _events = [];

    private DispatchRun(
        Guid id,
        DateTimeOffset createdAt,
        Guid shipmentId,
        Guid loadPlanId,
        Guid routePlanId,
        Guid vehicleId,
        Guid driverId,
        DateTimeOffset? plannedDepartureAt,
        IReadOnlyCollection<DispatchRunStop> stops)
        : base(id, createdAt)
    {
        ShipmentId = shipmentId;
        LoadPlanId = loadPlanId;
        RoutePlanId = routePlanId;
        VehicleId = vehicleId;
        DriverId = driverId;
        PlannedDepartureAt = plannedDepartureAt;
        Status = DispatchRunStatus.Prepared;
        _stops = stops.OrderBy(x => x.SequenceNo).ThenBy(x => x.RouteStopId).ToList();
    }

    public Guid ShipmentId { get; }
    public Guid LoadPlanId { get; }
    public Guid RoutePlanId { get; }
    public Guid VehicleId { get; }
    public Guid DriverId { get; }
    public DispatchRunStatus Status { get; private set; }
    public DateTimeOffset? PlannedDepartureAt { get; }
    public DateTimeOffset? ActualDepartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? DispatchedBy { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public string? ExceptionReason { get; private set; }
    public IReadOnlyCollection<DispatchRunStop> Stops => _stops.AsReadOnly();
    public IReadOnlyCollection<RouteExecutionEvent> Events => _events.AsReadOnly();

    public static DispatchRun CreatePrepared(
        Guid id,
        DateTimeOffset now,
        Guid shipmentId,
        Guid loadPlanId,
        Guid routePlanId,
        Guid vehicleId,
        Guid driverId,
        DateTimeOffset? plannedDepartureAt,
        IReadOnlyCollection<DispatchRunStop> stops)
    {
        DomainGuard.AgainstEmpty(id, "DISPATCH_RUN_ID_REQUIRED", "DispatchRun kimliği zorunludur.");
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "DispatchRun shipment kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "DispatchRun LoadPlan kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(routePlanId, "ROUTE_PLAN_REQUIRED", "DispatchRun RoutePlan kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(vehicleId, "VEHICLE_REQUIRED", "DispatchRun vehicle kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(driverId, "DRIVER_REQUIRED", "DispatchRun driver kaydına bağlı olmalıdır.");
        ValidateStops(stops);

        return new DispatchRun(id, now, shipmentId, loadPlanId, routePlanId, vehicleId, driverId, plannedDepartureAt, stops);
    }

    public static DispatchRun Rehydrate(
        Guid id,
        DateTimeOffset createdAt,
        Guid shipmentId,
        Guid loadPlanId,
        Guid routePlanId,
        Guid vehicleId,
        Guid driverId,
        DispatchRunStatus status,
        DateTimeOffset? plannedDepartureAt,
        DateTimeOffset? actualDepartedAt,
        DateTimeOffset? completedAt,
        DateTimeOffset? cancelledAt,
        Guid? dispatchedBy,
        Guid? completedBy,
        Guid? cancelledBy,
        string? exceptionReason,
        DateTimeOffset updatedAt,
        IReadOnlyCollection<DispatchRunStop> stops,
        IReadOnlyCollection<RouteExecutionEvent> events)
    {
        DomainGuard.AgainstEmpty(id, "DISPATCH_RUN_ID_REQUIRED", "DispatchRun kimliği zorunludur.");
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "DispatchRun shipment kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "DispatchRun LoadPlan kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(routePlanId, "ROUTE_PLAN_REQUIRED", "DispatchRun RoutePlan kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(vehicleId, "VEHICLE_REQUIRED", "DispatchRun vehicle kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(driverId, "DRIVER_REQUIRED", "DispatchRun driver kaydına bağlı olmalıdır.");
        ValidateRehydratedStops(stops);

        var run = new DispatchRun(id, createdAt, shipmentId, loadPlanId, routePlanId, vehicleId, driverId, plannedDepartureAt, stops)
        {
            Status = status,
            ActualDepartedAt = actualDepartedAt,
            CompletedAt = completedAt,
            CancelledAt = cancelledAt,
            DispatchedBy = dispatchedBy,
            CompletedBy = completedBy,
            CancelledBy = cancelledBy,
            ExceptionReason = exceptionReason,
        };
        run._events.AddRange(events.OrderBy(x => x.SequenceNo));
        run.RestoreUpdatedAt(updatedAt);
        return run;
    }

    public void ConfirmDispatch(Guid actorId, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(actorId, "ACTOR_REQUIRED", "Dispatch onayı yapan kullanıcı zorunludur.");
        EnsureStatus(DispatchRunStatus.Prepared, "DISPATCH_INVALID_STATE", "Yalnızca Prepared DispatchRun dispatch olarak onaylanabilir.");

        Status = DispatchRunStatus.Dispatched;
        DispatchedBy = actorId;
        Touch(now);
    }

    public RouteExecutionEvent Depart(
        Guid actorId,
        DateTimeOffset occurredAt,
        string idempotencyKey,
        string correlationId,
        string? locationText = null,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        if (_events.Any(x => x.EventType == RouteExecutionEventType.Departed))
        {
            throw new DomainException(new("DISPATCH_ALREADY_DEPARTED", "DispatchRun departure daha önce gerçekleşmiştir."));
        }

        EnsureStatus(DispatchRunStatus.Dispatched, "DISPATCH_INVALID_STATE", "Yalnızca Dispatched DispatchRun departure yapabilir.");
        DomainGuard.AgainstEmpty(actorId, "ACTOR_REQUIRED", "Departure yapan kullanıcı zorunludur.");
        DomainGuard.AgainstBlank(idempotencyKey, "IDEMPOTENCY_KEY_REQUIRED", "Idempotency key zorunludur.");
        DomainGuard.AgainstBlank(correlationId, "CORRELATION_ID_REQUIRED", "Correlation ID zorunludur.");
        ValidateLocation(latitude, longitude);
        EnsureNoEventOfType(RouteExecutionEventType.Departed);

        var routeEvent = CreateEvent(
            RouteExecutionEventType.Departed,
            null,
            actorId,
            occurredAt,
            idempotencyKey,
            correlationId,
            locationText,
            latitude,
            longitude,
            null);

        Status = DispatchRunStatus.InTransit;
        ActualDepartedAt = occurredAt;
        _events.Add(routeEvent);
        Touch(occurredAt);
        return routeEvent;
    }

    public RouteExecutionEvent ArriveAtStop(
        Guid routeStopId,
        Guid actorId,
        DateTimeOffset occurredAt,
        string idempotencyKey,
        string correlationId,
        string? locationText = null,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        EnsureStatus(DispatchRunStatus.InTransit, "DISPATCH_INVALID_STATE", "Yalnızca InTransit DispatchRun stop arrival yapabilir.");
        var stop = GetNextPendingStop(routeStopId);
        ValidateLocation(latitude, longitude);

        var routeEvent = CreateEvent(
            RouteExecutionEventType.ArrivedAtStop,
            routeStopId,
            actorId,
            occurredAt,
            idempotencyKey,
            correlationId,
            locationText,
            latitude,
            longitude,
            null);

        ReplaceStop(stop.WithStatus(RouteStopExecutionStatus.Arrived));
        _events.Add(routeEvent);
        Touch(occurredAt);
        return routeEvent;
    }

    public RouteExecutionEvent DepartStop(
        Guid routeStopId,
        Guid actorId,
        DateTimeOffset occurredAt,
        string idempotencyKey,
        string correlationId,
        string? locationText = null,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        EnsureStatus(DispatchRunStatus.InTransit, "DISPATCH_INVALID_STATE", "Yalnızca InTransit DispatchRun stop departure yapabilir.");
        var stop = GetStop(routeStopId);
        if (stop.Status != RouteStopExecutionStatus.Arrived)
        {
            throw new DomainException(new("ROUTE_STOP_INVALID_STATE", "Stop yalnızca Arrived durumundan Departed durumuna geçebilir."));
        }

        ValidateLocation(latitude, longitude);
        var routeEvent = CreateEvent(
            RouteExecutionEventType.DepartedStop,
            routeStopId,
            actorId,
            occurredAt,
            idempotencyKey,
            correlationId,
            locationText,
            latitude,
            longitude,
            null);

        ReplaceStop(stop.WithStatus(RouteStopExecutionStatus.Departed));
        _events.Add(routeEvent);
        Touch(occurredAt);
        return routeEvent;
    }

    public RouteExecutionEvent SkipStop(
        Guid routeStopId,
        Guid actorId,
        DateTimeOffset occurredAt,
        string reason,
        string idempotencyKey,
        string correlationId)
    {
        EnsureStatus(DispatchRunStatus.InTransit, "DISPATCH_INVALID_STATE", "Yalnızca InTransit DispatchRun stop skip yapabilir.");
        DomainGuard.AgainstBlank(reason, "ROUTE_STOP_REASON_REQUIRED", "Stop skip gerekçesi zorunludur.");
        var stop = GetNextPendingStop(routeStopId);

        var routeEvent = CreateEvent(
            RouteExecutionEventType.SkippedStop,
            routeStopId,
            actorId,
            occurredAt,
            idempotencyKey,
            correlationId,
            null,
            null,
            null,
            reason.Trim());

        ReplaceStop(stop.WithStatus(RouteStopExecutionStatus.Skipped));
        _events.Add(routeEvent);
        Touch(occurredAt);
        return routeEvent;
    }

    public RouteExecutionEvent CompleteRoute(
        Guid actorId,
        DateTimeOffset occurredAt,
        string idempotencyKey,
        string correlationId)
    {
        EnsureStatus(DispatchRunStatus.InTransit, "DISPATCH_INVALID_STATE", "Yalnızca InTransit DispatchRun route complete yapabilir.");
        if (_stops.Any(x => x.Status is not (RouteStopExecutionStatus.Departed or RouteStopExecutionStatus.Skipped)))
        {
            throw new DomainException(new("ROUTE_NOT_COMPLETE", "Açık route stop varken DispatchRun tamamlanamaz."));
        }

        var routeEvent = CreateEvent(
            RouteExecutionEventType.RouteCompleted,
            null,
            actorId,
            occurredAt,
            idempotencyKey,
            correlationId,
            null,
            null,
            null,
            null);

        Status = DispatchRunStatus.Completed;
        CompletedAt = occurredAt;
        CompletedBy = actorId;
        _events.Add(routeEvent);
        Touch(occurredAt);
        return routeEvent;
    }

    public RouteExecutionEvent Cancel(
        Guid actorId,
        DateTimeOffset occurredAt,
        string reason,
        string idempotencyKey,
        string correlationId)
    {
        if (Status is not (DispatchRunStatus.Prepared or DispatchRunStatus.Dispatched))
        {
            throw new DomainException(new("DISPATCH_INVALID_STATE", "Yalnızca departure öncesi DispatchRun iptal edilebilir."));
        }

        DomainGuard.AgainstBlank(reason, "ROUTE_STOP_REASON_REQUIRED", "Dispatch iptal gerekçesi zorunludur.");
        var routeEvent = CreateEvent(
            RouteExecutionEventType.Cancelled,
            null,
            actorId,
            occurredAt,
            idempotencyKey,
            correlationId,
            null,
            null,
            null,
            reason.Trim());

        Status = DispatchRunStatus.Cancelled;
        CancelledAt = occurredAt;
        CancelledBy = actorId;
        ExceptionReason = reason.Trim();
        _events.Add(routeEvent);
        Touch(occurredAt);
        return routeEvent;
    }

    private RouteExecutionEvent CreateEvent(
        RouteExecutionEventType eventType,
        Guid? routeStopId,
        Guid actorId,
        DateTimeOffset occurredAt,
        string idempotencyKey,
        string correlationId,
        string? locationText,
        decimal? latitude,
        decimal? longitude,
        string? reason)
    {
        DomainGuard.AgainstEmpty(actorId, "ACTOR_REQUIRED", "Event actor zorunludur.");
        DomainGuard.AgainstBlank(idempotencyKey, "IDEMPOTENCY_KEY_REQUIRED", "Idempotency key zorunludur.");
        DomainGuard.AgainstBlank(correlationId, "CORRELATION_ID_REQUIRED", "Correlation ID zorunludur.");
        if (_events.Any(x => string.Equals(x.IdempotencyKey, idempotencyKey.Trim(), StringComparison.Ordinal)))
        {
            throw new DomainException(new("ROUTE_EXECUTION_IDEMPOTENCY_MISMATCH", "Aynı DispatchRun içinde idempotency key tekrar kullanılamaz."));
        }

        var previous = _events.LastOrDefault();
        if (previous is not null && occurredAt < previous.OccurredAt)
        {
            throw new DomainException(new("ROUTE_EXECUTION_TIME_ORDER_INVALID", "Route event zamanı önceki event zamanından geri olamaz."));
        }

        return new RouteExecutionEvent(
            Guid.NewGuid(),
            Id,
            RoutePlanId,
            routeStopId,
            eventType,
            _events.Count + 1L,
            occurredAt,
            actorId,
            locationText,
            latitude,
            longitude,
            reason,
            idempotencyKey.Trim(),
            correlationId.Trim());
    }

    private DispatchRunStop GetNextPendingStop(Guid routeStopId)
    {
        var next = _stops.FirstOrDefault(x => x.Status == RouteStopExecutionStatus.Pending);
        if (next is null)
        {
            throw new DomainException(new("ROUTE_NOT_COMPLETE", "Açık pending stop bulunmuyor."));
        }

        if (next.RouteStopId != routeStopId)
        {
            throw new DomainException(new("ROUTE_STOP_OUT_OF_ORDER", "Yalnızca sıradaki route stop işlenebilir."));
        }

        return next;
    }

    private DispatchRunStop GetStop(Guid routeStopId)
    {
        var stop = _stops.FirstOrDefault(x => x.RouteStopId == routeStopId);
        return stop ?? throw new DomainException(new("ROUTE_STOP_NOT_FOUND", "DispatchRun route stop kaydını içermiyor."));
    }

    private void ReplaceStop(DispatchRunStop replacement)
    {
        var index = _stops.FindIndex(x => x.RouteStopId == replacement.RouteStopId);
        _stops[index] = replacement;
    }

    private void EnsureNoEventOfType(RouteExecutionEventType eventType)
    {
        if (_events.Any(x => x.EventType == eventType))
        {
            throw new DomainException(new("DISPATCH_ALREADY_DEPARTED", "DispatchRun departure daha önce gerçekleşmiştir."));
        }
    }

    private void EnsureStatus(DispatchRunStatus expected, string code, string message)
    {
        if (Status != expected)
        {
            throw new DomainException(new(code, message));
        }
    }

    private static void ValidateStops(IReadOnlyCollection<DispatchRunStop> stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Count == 0)
        {
            throw new DomainException(new("ROUTE_STOPS_REQUIRED", "DispatchRun en az bir route stop içermelidir."));
        }

        if (stops.Any(x => x.RouteStopId == Guid.Empty || x.SequenceNo <= 0))
        {
            throw new DomainException(new("ROUTE_STOP_INVALID", "Route stop kimliği ve sequence değeri geçerli olmalıdır."));
        }

        if (stops.Select(x => x.SequenceNo).Distinct().Count() != stops.Count
            || stops.Select(x => x.RouteStopId).Distinct().Count() != stops.Count)
        {
            throw new DomainException(new("ROUTE_STOP_DUPLICATE", "Route stop sequence ve kimlikleri unique olmalıdır."));
        }

        if (stops.Any(x => x.Status != RouteStopExecutionStatus.Pending))
        {
            throw new DomainException(new("ROUTE_STOP_INITIAL_STATE_INVALID", "Yeni DispatchRun stop’ları Pending olmalıdır."));
        }
    }

    private static void ValidateRehydratedStops(IReadOnlyCollection<DispatchRunStop> stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Count == 0 || stops.Any(x => x.RouteStopId == Guid.Empty || x.SequenceNo <= 0))
        {
            throw new DomainException(new("ROUTE_STOP_INVALID", "Rehydrate edilen route stop değerleri geçerli olmalıdır."));
        }

        if (stops.Select(x => x.SequenceNo).Distinct().Count() != stops.Count
            || stops.Select(x => x.RouteStopId).Distinct().Count() != stops.Count)
        {
            throw new DomainException(new("ROUTE_STOP_DUPLICATE", "Rehydrate edilen route stop sequence ve kimlikleri unique olmalıdır."));
        }
    }

    private static void ValidateLocation(decimal? latitude, decimal? longitude)
    {
        if (latitude is null && longitude is null)
        {
            return;
        }

        if (latitude is null || longitude is null || latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            throw new DomainException(new("ROUTE_LOCATION_INVALID", "Latitude ve longitude birlikte geçerli aralıkta verilmelidir."));
        }
    }
}
