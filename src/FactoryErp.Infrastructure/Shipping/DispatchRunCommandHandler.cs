using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Shipping;

/// <summary>
/// L4-B6 transactional CQRS handler.
/// Lock order: LoadPlan -> Shipment -> RoutePlan -> DispatchRun -> Vehicle -> Driver -> RouteStops.
/// </summary>
public sealed class DispatchRunCommandHandler(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IDispatchRunCommandHandler
{
    public async Task<DispatchRunDto> HandleAsync(
        PrepareDispatchRunCommand command,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(command.ShipmentId, "SHIPMENT_REQUIRED", "Shipment zorunludur.");
        DomainGuard.AgainstEmpty(command.LoadPlanId, "LOAD_PLAN_REQUIRED", "LoadPlan zorunludur.");
        DomainGuard.AgainstEmpty(command.RoutePlanId, "ROUTE_PLAN_REQUIRED", "RoutePlan zorunludur.");
        DomainGuard.AgainstEmpty(command.VehicleId, "VEHICLE_REQUIRED", "Vehicle zorunludur.");
        DomainGuard.AgainstEmpty(command.DriverId, "DRIVER_REQUIRED", "Driver zorunludur.");
        DomainGuard.AgainstEmpty(command.ActorId, "ACTOR_REQUIRED", "Command actor zorunludur.");

        var scope = $"dispatch-run:prepare:{command.ActorId}:{command.ShipmentId}";
        var payloadHash = ComputePayloadHash(command);
        var replay = await TryReplayAsync<DispatchRunDto>(scope, command.IdempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var loadPlan = await LockLoadPlanAsync(command.LoadPlanId, cancellationToken)
            ?? throw NotFound("LOAD_PLAN_NOT_FOUND", "LoadPlan bulunamadı.");
        EnsureExpectedVersion(loadPlan.RowVersion, command.ExpectedLoadPlanRowVersion, nameof(LoadPlanRecord), loadPlan.Id);

        var shipment = await LockShipmentAsync(command.ShipmentId, cancellationToken)
            ?? throw NotFound("SHIPMENT_NOT_FOUND", "Shipment bulunamadı.");
        EnsureExpectedVersion(shipment.RowVersion, command.ExpectedShipmentRowVersion, nameof(ShipmentRecord), shipment.Id);

        var routePlan = await LockRoutePlanAsync(command.RoutePlanId, cancellationToken)
            ?? throw NotFound("ROUTE_PLAN_NOT_FOUND", "RoutePlan bulunamadı.");
        EnsureExpectedVersion(routePlan.RowVersion, command.ExpectedRoutePlanRowVersion, nameof(RoutePlanRecord), routePlan.Id);
        var stops = await LockRouteStopsAsync(routePlan.Id, cancellationToken);
        var vehicle = await LockVehicleAsync(command.VehicleId, cancellationToken)
            ?? throw NotFound("VEHICLE_NOT_FOUND", "Vehicle bulunamadı.");
        var driver = await LockDriverAsync(command.DriverId, cancellationToken)
            ?? throw NotFound("DRIVER_NOT_FOUND", "Driver bulunamadı.");
        await EnsureNoActiveRunAsync(command.ShipmentId, command.RoutePlanId, command.VehicleId, command.DriverId, cancellationToken);

        EnsurePreparePreconditions(command, loadPlan, shipment, routePlan, stops, vehicle, driver);

        var now = DateTimeOffset.UtcNow;
        var domainStops = stops
            .Select(x => new DispatchRunStop(x.Id, x.SequenceNo))
            .ToArray();
        EnsureCommandStopsMatch(command.Stops, domainStops);
        var run = DispatchRun.CreatePrepared(
            Guid.NewGuid(),
            now,
            shipment.Id,
            loadPlan.Id,
            routePlan.Id,
            vehicle.Id,
            driver.Id,
            command.PlannedDepartureAt,
            domainStops);
        vehicle.Status = "Assigned";
        vehicle.CurrentRoutePlanId = routePlan.Id;
        vehicle.LastStatusAt = now;
        vehicle.RowVersion++;
        routePlan.VehicleId = vehicle.Id;
        routePlan.DriverId = driver.Id;
        routePlan.UpdatedAt = now;
        routePlan.RowVersion++;

        var record = ToRecord(run, command.ActorId, now);
        dbContext.DispatchRuns.Add(record);
        await auditWriter.AppendAsync(new(
            "DispatchRunPrepared",
            nameof(DispatchRunRecord),
            record.Id,
            command.ActorId,
            command.CorrelationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                record.ShipmentId,
                record.LoadPlanId,
                record.RoutePlanId,
                record.VehicleId,
                record.DriverId,
                record.Status,
            })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record, Array.Empty<RouteExecutionEventRecord>(), stops);
        await SaveIdempotencyAsync(scope, command.IdempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<DispatchRunDto> HandleAsync(
        ConfirmDispatchCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = $"dispatch-run:confirm:{command.ActorId}:{command.DispatchRunId}";
        var payloadHash = ComputePayloadHash(command);
        var replay = await TryReplayAsync<DispatchRunDto>(scope, command.IdempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var graph = await LockGraphAsync(command.DispatchRunId, cancellationToken);
        EnsureExpectedVersion(graph.Run.RowVersion, command.ExpectedDispatchRunRowVersion, nameof(DispatchRunRecord), graph.Run.Id);
        var domain = Rehydrate(graph);
        domain.ConfirmDispatch(command.ActorId, DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        ApplyRun(domain, graph.Run, now);
        await AppendAuditAsync("DispatchRunConfirmed", graph.Run, command.ActorId, command.CorrelationId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(graph.Run, graph.Events, graph.Stops);
        await SaveIdempotencyAsync(scope, command.IdempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<DispatchRunDto> HandleAsync(
        DepartDispatchRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = $"dispatch-run:depart:{command.ActorId}:{command.DispatchRunId}";
        var payloadHash = ComputePayloadHash(command);
        var replay = await TryReplayAsync<DispatchRunDto>(scope, command.IdempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var graph = await LockGraphAsync(command.DispatchRunId, cancellationToken);
        EnsureExpectedVersion(graph.Run.RowVersion, command.ExpectedDispatchRunRowVersion, nameof(DispatchRunRecord), graph.Run.Id);
        EnsureDeparturePreconditions(graph, command.OccurredAt);
        var domain = Rehydrate(graph);
        var routeEvent = domain.Depart(
            command.ActorId,
            command.OccurredAt,
            command.IdempotencyKey,
            command.CorrelationId,
            command.LocationText,
            command.Latitude,
            command.Longitude);
        var now = DateTimeOffset.UtcNow;
        ApplyRun(domain, graph.Run, now);
        ApplyDepartureProjection(graph, command, now);
        AddEvent(graph, routeEvent, now);
        await AppendAuditAsync("DispatchRunDeparted", graph.Run, command.ActorId, command.CorrelationId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(graph.Run, graph.Events, graph.Stops);
        await SaveIdempotencyAsync(scope, command.IdempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task<DispatchRunDto> HandleAsync(
        DeliverStopCommand command,
        CancellationToken cancellationToken = default)
        => ExecuteStopEventAsync(
            command.DispatchRunId,
            command.ExpectedDispatchRunRowVersion,
            command.ActorId,
            command.IdempotencyKey,
            command.CorrelationId,
            command,
            RouteExecutionEventType.DeliveredStop,
            (domain, stops, now) =>
            {
                var routeEvent = domain.DeliverStop(
                    command.RouteStopId,
                    command.ActorId,
                    command.OccurredAt,
                    command.RecipientName,
                    command.Note,
                    command.IdempotencyKey,
                    command.CorrelationId);
                return (routeEvent, command.RouteStopId, "Delivered", command.OccurredAt, command.RecipientName.Trim());
            },
            cancellationToken);

    public Task<DispatchRunDto> HandleAsync(
        ArriveAtStopCommand command,
        CancellationToken cancellationToken = default)
        => ExecuteStopEventAsync(
            command.DispatchRunId,
            command.ExpectedDispatchRunRowVersion,
            command.ActorId,
            command.IdempotencyKey,
            command.CorrelationId,
            command,
            RouteExecutionEventType.ArrivedAtStop,
            (domain, stops, now) =>
            {
                var routeEvent = domain.ArriveAtStop(command.RouteStopId, command.ActorId, command.OccurredAt, command.IdempotencyKey, command.CorrelationId, command.LocationText, command.Latitude, command.Longitude);
                return (routeEvent, command.RouteStopId, "Arrived", command.OccurredAt, null);
            },
            cancellationToken);

    public Task<DispatchRunDto> HandleAsync(
        DepartStopCommand command,
        CancellationToken cancellationToken = default)
        => ExecuteStopEventAsync(
            command.DispatchRunId,
            command.ExpectedDispatchRunRowVersion,
            command.ActorId,
            command.IdempotencyKey,
            command.CorrelationId,
            command,
            RouteExecutionEventType.DepartedStop,
            (domain, stops, now) =>
            {
                var routeEvent = domain.DepartStop(command.RouteStopId, command.ActorId, command.OccurredAt, command.IdempotencyKey, command.CorrelationId, command.LocationText, command.Latitude, command.Longitude);
                return (routeEvent, command.RouteStopId, "Departed", command.OccurredAt, null);
            },
            cancellationToken);

    public Task<DispatchRunDto> HandleAsync(
        SkipStopCommand command,
        CancellationToken cancellationToken = default)
        => ExecuteStopEventAsync(
            command.DispatchRunId,
            command.ExpectedDispatchRunRowVersion,
            command.ActorId,
            command.IdempotencyKey,
            command.CorrelationId,
            command,
            RouteExecutionEventType.SkippedStop,
            (domain, stops, now) =>
            {
                var routeEvent = domain.SkipStop(command.RouteStopId, command.ActorId, command.OccurredAt, command.Reason, command.IdempotencyKey, command.CorrelationId);
                return (routeEvent, command.RouteStopId, "Skipped", command.OccurredAt, command.Reason.Trim());
            },
            cancellationToken);

    public async Task<DispatchRunDto> HandleAsync(
        CompleteRouteCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = $"dispatch-run:complete:{command.ActorId}:{command.DispatchRunId}";
        var payloadHash = ComputePayloadHash(command);
        var replay = await TryReplayAsync<DispatchRunDto>(scope, command.IdempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var graph = await LockGraphAsync(command.DispatchRunId, cancellationToken);
        EnsureExpectedVersion(graph.Run.RowVersion, command.ExpectedDispatchRunRowVersion, nameof(DispatchRunRecord), graph.Run.Id);
        var domain = Rehydrate(graph);
        var routeEvent = domain.CompleteRoute(command.ActorId, command.OccurredAt, command.IdempotencyKey, command.CorrelationId);
        var now = DateTimeOffset.UtcNow;
        ApplyRun(domain, graph.Run, now);
        graph.RoutePlan.Status = "Completed";
        graph.RoutePlan.UpdatedAt = now;
        graph.RoutePlan.RowVersion++;
        graph.Vehicle.Status = "Available";
        graph.Vehicle.CurrentRoutePlanId = null;
        graph.Vehicle.LastStatusAt = now;
        graph.Vehicle.RowVersion++;
        ApplyShipmentDeliveryProjection(graph);
        AddEvent(graph, routeEvent, now);
        await AppendAuditAsync("DispatchRunCompleted", graph.Run, command.ActorId, command.CorrelationId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(graph.Run, graph.Events, graph.Stops);
        await SaveIdempotencyAsync(scope, command.IdempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<DispatchRunDto> HandleAsync(
        CancelDispatchRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = $"dispatch-run:cancel:{command.ActorId}:{command.DispatchRunId}";
        var payloadHash = ComputePayloadHash(command);
        var replay = await TryReplayAsync<DispatchRunDto>(scope, command.IdempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var graph = await LockGraphAsync(command.DispatchRunId, cancellationToken);
        EnsureExpectedVersion(graph.Run.RowVersion, command.ExpectedDispatchRunRowVersion, nameof(DispatchRunRecord), graph.Run.Id);
        var domain = Rehydrate(graph);
        var routeEvent = domain.Cancel(command.ActorId, command.OccurredAt, command.Reason, command.IdempotencyKey, command.CorrelationId);
        var now = DateTimeOffset.UtcNow;
        ApplyRun(domain, graph.Run, now);
        if (graph.Vehicle.CurrentRoutePlanId == graph.RoutePlan.Id)
        {
            graph.Vehicle.CurrentRoutePlanId = null;
            if (graph.Vehicle.Status == "Assigned")
            {
                graph.Vehicle.Status = "Available";
            }
            graph.Vehicle.LastStatusAt = now;
            graph.Vehicle.RowVersion++;
        }
        AddEvent(graph, routeEvent, now);
        await AppendAuditAsync("DispatchRunCancelled", graph.Run, command.ActorId, command.CorrelationId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(graph.Run, graph.Events, graph.Stops);
        await SaveIdempotencyAsync(scope, command.IdempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<DispatchRunDto> ExecuteStopEventAsync<TCommand>(
        Guid dispatchRunId,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        TCommand command,
        RouteExecutionEventType eventType,
        Func<DispatchRun, IReadOnlyCollection<RouteStopRecord>, DateTimeOffset, (RouteExecutionEvent Event, Guid RouteStopId, string Status, DateTimeOffset? OccurredAt, string? Reason)> apply,
        CancellationToken cancellationToken)
    {
        var scope = $"dispatch-run:{eventType}:{actorId}:{dispatchRunId}";
        var payloadHash = ComputePayloadHash(command);
        var replay = await TryReplayAsync<DispatchRunDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var graph = await LockGraphAsync(dispatchRunId, cancellationToken);
        EnsureExpectedVersion(graph.Run.RowVersion, expectedRowVersion, nameof(DispatchRunRecord), graph.Run.Id);
        var domain = Rehydrate(graph);
        var applied = apply(domain, graph.Stops, DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        ApplyRun(domain, graph.Run, now);
        var stop = graph.Stops.Single(x => x.Id == applied.RouteStopId);
        stop.Status = applied.Status;
        stop.RowVersion++;
        if (eventType == RouteExecutionEventType.ArrivedAtStop)
        {
            stop.ActualArrivalAt = applied.OccurredAt;
        }
        else if (eventType == RouteExecutionEventType.DepartedStop)
        {
            stop.ActualDepartureAt = applied.OccurredAt;
        }
        else if (eventType == RouteExecutionEventType.DeliveredStop)
        {
            stop.DeliveredAt = applied.OccurredAt;
            stop.ProofRecipient = applied.Reason;
            var delivered = domain.Stops.Single(x => x.RouteStopId == applied.RouteStopId);
            stop.ProofNote = delivered.ProofNote;
        }
        else if (eventType == RouteExecutionEventType.SkippedStop)
        {
            stop.SkippedAt = applied.OccurredAt;
            stop.ExceptionReason = applied.Reason;
        }
        AddEvent(graph, applied.Event, now);
        await AppendAuditAsync($"RouteStop{eventType}", graph.Run, actorId, correlationId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(graph.Run, graph.Events, graph.Stops);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<LockedGraph> LockGraphAsync(Guid dispatchRunId, CancellationToken cancellationToken)
    {
        var runReference = await dbContext.DispatchRuns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == dispatchRunId, cancellationToken)
            ?? throw NotFound("DISPATCH_RUN_NOT_FOUND", "DispatchRun bulunamadı.");
        var loadPlan = await LockLoadPlanAsync(runReference.LoadPlanId, cancellationToken)
            ?? throw NotFound("LOAD_PLAN_NOT_FOUND", "DispatchRun LoadPlan kaydı bulunamadı.");
        var shipment = await LockShipmentAsync(runReference.ShipmentId, cancellationToken)
            ?? throw NotFound("SHIPMENT_NOT_FOUND", "DispatchRun Shipment kaydı bulunamadı.");
        var routePlan = await LockRoutePlanAsync(runReference.RoutePlanId, cancellationToken)
            ?? throw NotFound("ROUTE_PLAN_NOT_FOUND", "DispatchRun RoutePlan kaydı bulunamadı.");
        var run = await LockDispatchRunAsync(dispatchRunId, cancellationToken)
            ?? throw NotFound("DISPATCH_RUN_NOT_FOUND", "DispatchRun bulunamadı.");
        var vehicle = await LockVehicleAsync(run.VehicleId, cancellationToken)
            ?? throw NotFound("VEHICLE_NOT_FOUND", "DispatchRun vehicle kaydı bulunamadı.");
        var driver = await LockDriverAsync(run.DriverId, cancellationToken)
            ?? throw NotFound("DRIVER_NOT_FOUND", "DispatchRun driver kaydı bulunamadı.");
        var stops = await LockRouteStopsAsync(routePlan.Id, cancellationToken);
        var events = await dbContext.RouteExecutionEvents
            .Where(x => x.DispatchRunId == dispatchRunId)
            .OrderBy(x => x.SequenceNo)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return new LockedGraph(run, loadPlan, shipment, routePlan, vehicle, driver, stops, events);
    }

    private void EnsurePreparePreconditions(
        PrepareDispatchRunCommand command,
        LoadPlanRecord loadPlan,
        ShipmentRecord shipment,
        RoutePlanRecord routePlan,
        IReadOnlyCollection<RouteStopRecord> stops,
        VehicleRecord vehicle,
        DriverRecord driver)
    {
        if (loadPlan.ShipmentId != shipment.Id || loadPlan.RoutePlanId != routePlan.Id || routePlan.ShipmentId != shipment.Id)
        {
            throw new DomainException(new("DISPATCH_PRECONDITION_FAILED", "LoadPlan, Shipment ve RoutePlan ownership zinciri uyuşmuyor."));
        }
        if (loadPlan.Status != "Locked" || loadPlan.VehicleId != command.VehicleId || string.IsNullOrWhiteSpace(loadPlan.InputSnapshotHash) || loadPlan.VehicleCapacityId is null)
        {
            throw new DomainException(new("DISPATCH_PRECONDITION_FAILED", "LoadPlan Locked ve vehicle/capacity/snapshot ön koşullarını sağlamıyor."));
        }
        if (routePlan.Status != "Locked")
        {
            throw new DomainException(new("DISPATCH_PRECONDITION_FAILED", "RoutePlan dispatch için Locked olmalıdır."));
        }
        if (shipment.Status != "Loaded")
        {
            throw new DomainException(new("DISPATCH_PRECONDITION_FAILED", "Shipment actual-load verification sonrası Loaded olmalıdır."));
        }
        if (!dbContext.LoadVerificationSessions.Any(x => x.LoadPlanId == loadPlan.Id && x.ShipmentId == shipment.Id && x.Status == "Completed"))
        {
            throw new DomainException(new("DISPATCH_PRECONDITION_FAILED", "Tamamlanmış LoadVerificationSession bulunamadı."));
        }
        if (stops.Count == 0 || stops.Any(x => x.Status is not ("Pending" or "Arrived" or "Departed" or "Skipped")))
        {
            throw new DomainException(new("DISPATCH_PRECONDITION_FAILED", "RouteStop listesi dispatch için geçersizdir."));
        }
        if (vehicle.Status != "Available" || vehicle.MaintenanceUntil is not null && vehicle.MaintenanceUntil > DateTimeOffset.UtcNow)
        {
            throw new DomainException(new("VEHICLE_NOT_AVAILABLE", "Vehicle Available durumda değil veya bakım süresi devam ediyor."));
        }
        if (!driver.IsActive || driver.Status != "Active")
        {
            throw new DomainException(new("DRIVER_NOT_AVAILABLE", "Driver aktif değil."));
        }
        var departureDate = DateOnly.FromDateTime((command.PlannedDepartureAt ?? DateTimeOffset.UtcNow).DateTime);
        if (driver.LicenseExpiry < departureDate)
        {
            throw new DomainException(new("DRIVER_LICENSE_EXPIRED", "Driver lisansı departure tarihinde geçerli değil."));
        }
    }

    private static void EnsureDeparturePreconditions(LockedGraph graph, DateTimeOffset occurredAt)
    {
        if (graph.Run.Status != "Dispatched" || graph.RoutePlan.Status != "Locked")
        {
            throw new DomainException(new("DISPATCH_PRECONDITION_FAILED", "Dispatch departure için run Dispatched ve RoutePlan Locked olmalıdır."));
        }
        if (graph.Shipment.Status != "Loaded")
        {
            throw new DomainException(new("DISPATCH_PRECONDITION_FAILED", "Departure öncesi Shipment Loaded olmalıdır."));
        }
        if (graph.Vehicle.Status != "Available" && graph.Vehicle.Status != "Assigned")
        {
            throw new DomainException(new("VEHICLE_NOT_AVAILABLE", "Vehicle departure için uygun değil."));
        }
        if (!graph.Driver.IsActive || graph.Driver.Status != "Active")
        {
            throw new DomainException(new("DRIVER_NOT_AVAILABLE", "Driver departure için uygun değil."));
        }
        if (graph.Driver.LicenseExpiry < DateOnly.FromDateTime(occurredAt.DateTime))
        {
            throw new DomainException(new("DRIVER_LICENSE_EXPIRED", "Driver lisansı departure tarihinde geçerli değil."));
        }
        if (graph.Stops.Count == 0)
        {
            throw new DomainException(new("DISPATCH_PRECONDITION_FAILED", "Departure için route stop bulunmalıdır."));
        }
    }

    private static void EnsureCommandStopsMatch(IReadOnlyCollection<DispatchStopInput> requested, IReadOnlyCollection<DispatchRunStop> actual)
    {
        if (requested.Count != actual.Count || requested.OrderBy(x => x.SequenceNo).Select(x => (x.RouteStopId, x.SequenceNo)).SequenceEqual(actual.OrderBy(x => x.SequenceNo).Select(x => (x.RouteStopId, x.SequenceNo))) == false)
        {
            throw new DomainException(new("ROUTE_STOP_SET_MISMATCH", "Command route stop seti RoutePlan ile aynı olmalıdır."));
        }
    }

    private static DispatchRun Rehydrate(LockedGraph graph)
    {
        return DispatchRun.Rehydrate(
            graph.Run.Id,
            graph.Run.CreatedAt,
            graph.Run.ShipmentId,
            graph.Run.LoadPlanId,
            graph.Run.RoutePlanId,
            graph.Run.VehicleId,
            graph.Run.DriverId,
            ParseEnum<DispatchRunStatus>(graph.Run.Status, "DISPATCH_STATUS_INVALID"),
            graph.Run.PlannedDepartureAt,
            graph.Run.ActualDepartedAt,
            graph.Run.CompletedAt,
            graph.Run.CancelledAt,
            graph.Run.DispatchedBy,
            graph.Run.CompletedBy,
            graph.Run.CancelledBy,
            graph.Run.ExceptionReason,
            graph.Run.UpdatedAt,
            graph.Stops.Select(x => new DispatchRunStop(x.Id, x.SequenceNo, ToDomainStopStatus(x.Status), x.ProofRecipient, x.ProofNote)).ToArray(),
            graph.Events.Select(ToDomainEvent).ToArray());
    }

    private static RouteStopExecutionStatus ToDomainStopStatus(string status) => status switch
    {
        "Pending" => RouteStopExecutionStatus.Pending,
        "Arrived" => RouteStopExecutionStatus.Arrived,
        "Delivered" => RouteStopExecutionStatus.Delivered,
        "Departed" => RouteStopExecutionStatus.Departed,
        "Skipped" => RouteStopExecutionStatus.Skipped,
        _ => throw new DomainException(new("ROUTE_STOP_INVALID_STATE", $"RouteStop status B6 için desteklenmiyor: {status}")),
    };

    private static RouteExecutionEvent ToDomainEvent(RouteExecutionEventRecord record)
        => new(
            record.Id,
            record.DispatchRunId,
            record.RoutePlanId,
            record.RouteStopId,
            ParseEnum<RouteExecutionEventType>(record.EventType, "ROUTE_EVENT_TYPE_INVALID"),
            record.SequenceNo,
            record.OccurredAt,
            record.ActorId,
            record.LocationText,
            record.Latitude,
            record.Longitude,
            record.Reason,
            record.IdempotencyKey,
            record.CorrelationId);

    private static DispatchRunRecord ToRecord(DispatchRun run, Guid actorId, DateTimeOffset now)
        => new()
        {
            Id = run.Id,
            ShipmentId = run.ShipmentId,
            LoadPlanId = run.LoadPlanId,
            RoutePlanId = run.RoutePlanId,
            VehicleId = run.VehicleId,
            DriverId = run.DriverId,
            Status = run.Status.ToString(),
            PlannedDepartureAt = run.PlannedDepartureAt,
            CreatedBy = actorId,
            RowVersion = 1,
            CreatedAt = run.CreatedAt,
            UpdatedAt = now,
        };

    private static void ApplyRun(DispatchRun run, DispatchRunRecord record, DateTimeOffset now)
    {
        record.Status = run.Status.ToString();
        record.ActualDepartedAt = run.ActualDepartedAt;
        record.CompletedAt = run.CompletedAt;
        record.CancelledAt = run.CancelledAt;
        record.DispatchedBy = run.DispatchedBy;
        record.CompletedBy = run.CompletedBy;
        record.CancelledBy = run.CancelledBy;
        record.ExceptionReason = run.ExceptionReason;
        record.UpdatedAt = now;
        record.RowVersion++;
    }

    private static void ApplyShipmentDeliveryProjection(LockedGraph graph)
    {
        var actionable = graph.Stops.Where(x => x.Status != "Skipped").ToArray();
        var delivered = actionable.Count(x => x.Status == "Delivered" || !string.IsNullOrWhiteSpace(x.ProofRecipient));
        if (actionable.Length > 0 && delivered == actionable.Length)
        {
            graph.Shipment.Status = "Delivered";
            graph.Shipment.RowVersion++;
        }
        else if (delivered > 0)
        {
            graph.Shipment.Status = "PartiallyDelivered";
            graph.Shipment.RowVersion++;
        }
    }

    public async Task<DispatchRunDto?> GetAsync(Guid dispatchRunId, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.DispatchRuns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == dispatchRunId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        return await MapPersistedAsync(record, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DispatchRunDto>> ListByShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.DispatchRuns
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        var result = new List<DispatchRunDto>(rows.Length);
        foreach (var row in rows)
        {
            result.Add(await MapPersistedAsync(row, cancellationToken));
        }

        return result;
    }

    private async Task<DispatchRunDto> MapPersistedAsync(DispatchRunRecord record, CancellationToken cancellationToken)
    {
        var stops = await dbContext.RouteStops
            .AsNoTracking()
            .Where(x => x.RoutePlanId == record.RoutePlanId)
            .OrderBy(x => x.SequenceNo)
            .ToArrayAsync(cancellationToken);
        var events = await dbContext.RouteExecutionEvents
            .AsNoTracking()
            .Where(x => x.DispatchRunId == record.Id)
            .OrderBy(x => x.SequenceNo)
            .ToArrayAsync(cancellationToken);
        return Map(record, events, stops);
    }

    private static void ApplyDepartureProjection(LockedGraph graph, DepartDispatchRunCommand command, DateTimeOffset now)
    {
        graph.Shipment.Status = "InTransit";
        graph.Shipment.RowVersion++;
        graph.RoutePlan.Status = "InProgress";
        graph.RoutePlan.UpdatedAt = now;
        graph.RoutePlan.RowVersion++;
        graph.Vehicle.Status = "InTransit";
        graph.Vehicle.CurrentRoutePlanId = graph.RoutePlan.Id;
        graph.Vehicle.LastKnownLocationText = command.LocationText;
        graph.Vehicle.LastStatusAt = now;
        graph.Vehicle.RowVersion++;
    }

    private void AddEvent(LockedGraph graph, RouteExecutionEvent routeEvent, DateTimeOffset now)
    {
        var record = new RouteExecutionEventRecord
        {
            Id = routeEvent.Id,
            DispatchRunId = routeEvent.DispatchRunId,
            RoutePlanId = routeEvent.RoutePlanId,
            RouteStopId = routeEvent.RouteStopId,
            EventType = routeEvent.EventType.ToString(),
            SequenceNo = routeEvent.SequenceNo,
            OccurredAt = routeEvent.OccurredAt,
            ActorId = routeEvent.ActorId,
            LocationText = routeEvent.LocationText,
            Latitude = routeEvent.Latitude,
            Longitude = routeEvent.Longitude,
            Reason = routeEvent.Reason,
            IdempotencyKey = routeEvent.IdempotencyKey,
            CorrelationId = routeEvent.CorrelationId,
            PayloadSnapshot = "{}",
            CreatedAt = now,
        };
        graph.Events.Add(record);
        dbContext.RouteExecutionEvents.Add(record);
    }

    private async Task AppendAuditAsync(string action, DispatchRunRecord run, Guid actorId, string correlationId, CancellationToken cancellationToken)
        => await auditWriter.AppendAsync(new(
            action,
            nameof(DispatchRunRecord),
            run.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { run.Status, run.ShipmentId, run.RoutePlanId, run.VehicleId, run.DriverId })), cancellationToken);

    private async Task EnsureNoActiveRunAsync(Guid shipmentId, Guid routePlanId, Guid vehicleId, Guid driverId, CancellationToken cancellationToken)
    {
        var activeRun = await dbContext.DispatchRuns
            .FromSqlInterpolated($"""
                SELECT * FROM dispatch_runs
                WHERE status IN ('Prepared', 'Dispatched', 'InTransit')
                  AND (shipment_id = {shipmentId} OR route_plan_id = {routePlanId} OR vehicle_id = {vehicleId} OR driver_id = {driverId})
                ORDER BY created_at, id
                FOR UPDATE
                """)
            .FirstOrDefaultAsync(cancellationToken);
        if (activeRun is not null)
        {
            throw new DomainException(new("DISPATCH_ACTIVE_RUN_EXISTS", $"Kaynaklardan biri aktif DispatchRun {activeRun.Id} tarafından rezerve edilmiş."));
        }
    }

    private Task<LoadPlanRecord?> LockLoadPlanAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.LoadPlans.FromSqlInterpolated($"SELECT * FROM load_plans WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

    private Task<ShipmentRecord?> LockShipmentAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Shipments.FromSqlInterpolated($"SELECT * FROM shipments WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

    private Task<RoutePlanRecord?> LockRoutePlanAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.RoutePlans.FromSqlInterpolated($"SELECT * FROM route_plans WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

    private Task<DispatchRunRecord?> LockDispatchRunAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.DispatchRuns.FromSqlInterpolated($"SELECT * FROM dispatch_runs WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

    private Task<VehicleRecord?> LockVehicleAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Vehicles.FromSqlInterpolated($"SELECT * FROM vehicles WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

    private Task<DriverRecord?> LockDriverAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Drivers.FromSqlInterpolated($"SELECT * FROM drivers WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

    private Task<List<RouteStopRecord>> LockRouteStopsAsync(Guid routePlanId, CancellationToken cancellationToken)
        => dbContext.RouteStops.FromSqlInterpolated($"SELECT * FROM route_stops WHERE route_plan_id = {routePlanId} ORDER BY sequence_no, id FOR UPDATE").ToListAsync(cancellationToken);

    private static DispatchRunDto Map(DispatchRunRecord record, IReadOnlyCollection<RouteExecutionEventRecord> events, IReadOnlyCollection<RouteStopRecord> stops)
        => new(
            record.Id,
            record.ShipmentId,
            record.LoadPlanId,
            record.RoutePlanId,
            record.VehicleId,
            record.DriverId,
            record.Status,
            record.PlannedDepartureAt,
            record.ActualDepartedAt,
            record.CompletedAt,
            record.CancelledAt,
            record.DispatchedBy,
            record.CompletedBy,
            record.CancelledBy,
            record.ExceptionReason,
            stops.OrderBy(x => x.SequenceNo).ThenBy(x => x.Id).Select(x => new DispatchRunStopDto(x.Id, x.SequenceNo, x.Status, x.ProofRecipient, x.ProofNote, x.DeliveredAt)).ToArray(),
            events.OrderBy(x => x.SequenceNo).ThenBy(x => x.Id).Select(x => new RouteExecutionEventDto(x.Id, x.DispatchRunId, x.RoutePlanId, x.RouteStopId, x.EventType, x.SequenceNo, x.OccurredAt, x.ActorId, x.LocationText, x.Latitude, x.Longitude, x.Reason, x.IdempotencyKey, x.CorrelationId)).ToArray(),
            record.CreatedAt,
            record.UpdatedAt,
            record.RowVersion);

    private async Task<T?> TryReplayAsync<T>(string scope, string key, string payloadHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException(new("MISSING_IDEMPOTENCY_KEY", "Idempotency-Key zorunludur."));
        }
        var stored = await idempotencyStore.FindAsync(scope, key, cancellationToken);
        if (stored is null)
        {
            return default;
        }
        if (!string.Equals(stored.PayloadHash, payloadHash, StringComparison.Ordinal))
        {
            throw new DomainException(new("IDEMPOTENCY_PAYLOAD_MISMATCH", "Aynı Idempotency-Key farklı payload ile kullanıldı."));
        }
        return JsonSerializer.Deserialize<T>(stored.ResponseBody)
            ?? throw new DomainException(new("IDEMPOTENCY_REPLAY_INVALID", "Idempotency replay sonucu okunamadı."));
    }

    private Task SaveIdempotencyAsync<T>(string scope, string key, string payloadHash, int statusCode, T result, CancellationToken cancellationToken)
        => idempotencyStore.SaveAsync(scope, key, payloadHash, statusCode, JsonSerializer.Serialize(result), DateTimeOffset.UtcNow.AddDays(30), cancellationToken);

    private static string ComputePayloadHash<T>(T payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));

    private static void EnsureExpectedVersion(long actual, long expected, string entityType, Guid entityId)
    {
        if (actual != expected)
        {
            throw new DomainException(new("RESOURCE_VERSION_CONFLICT", $"{entityType} {entityId} güncel değil; beklenen: {expected}, mevcut: {actual}."));
        }
    }

    private static T ParseEnum<T>(string value, string code) where T : struct, Enum
        => Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result)
            ? result
            : throw new DomainException(new(code, $"Geçersiz değer: {value}."));

    private static DomainException NotFound(string code, string message) => new(new(code, message));

    private sealed record LockedGraph(
        DispatchRunRecord Run,
        LoadPlanRecord LoadPlan,
        ShipmentRecord Shipment,
        RoutePlanRecord RoutePlan,
        VehicleRecord Vehicle,
        DriverRecord Driver,
        List<RouteStopRecord> Stops,
        List<RouteExecutionEventRecord> Events);
}
