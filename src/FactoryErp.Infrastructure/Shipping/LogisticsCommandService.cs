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

public sealed class LogisticsCommandService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : ILogisticsCommandService
{
    private static readonly string[] ActiveRouteStatuses = ["Draft", "Planned", "Locked", "InProgress"];

    public async Task<VehicleTypeDto> CreateVehicleTypeAsync(
        CreateVehicleTypeRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"vehicle-type:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<VehicleTypeDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var type = VehicleType.Create(Guid.NewGuid(), request.Code, request.Name);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = new VehicleTypeRecord
        {
            Id = type.Id,
            Code = type.Code,
            Name = type.Name,
            IsActive = type.IsActive,
        };
        dbContext.VehicleTypes.Add(record);
        await auditWriter.AppendAsync(new(
            "VehicleTypeCreated",
            nameof(VehicleTypeRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.Code, record.Name })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<VehicleTypeDto?> GetVehicleTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.VehicleTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<VehicleCapacityDto> CreateVehicleCapacityAsync(
        CreateVehicleCapacityRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"vehicle-capacity:create:{actorId}:{request.VehicleTypeId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<VehicleCapacityDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var vehicleType = await LockVehicleTypeAsync(request.VehicleTypeId, cancellationToken);
        if (vehicleType is null)
        {
            throw new DomainException(new("VEHICLE_TYPE_NOT_FOUND", "Aktif araç tipi bulunamadı."));
        }

        if (!vehicleType.IsActive)
        {
            throw new DomainException(new("VEHICLE_TYPE_INACTIVE", "Pasif araç tipine kapasite profili eklenemez."));
        }

        var overlap = await dbContext.VehicleCapacities.AsNoTracking().AnyAsync(
            x => x.VehicleTypeId == request.VehicleTypeId
                && x.EffectiveFrom < (request.EffectiveTo ?? DateTimeOffset.MaxValue)
                && (x.EffectiveTo ?? DateTimeOffset.MaxValue) > request.EffectiveFrom,
            cancellationToken);
        if (overlap)
        {
            throw new DomainException(new("CAPACITY_EFFECTIVE_OVERLAP", "Aynı araç tipinde kapasite geçerlilik aralıkları çakışamaz."));
        }

        var capacity = VehicleCapacity.Create(
            Guid.NewGuid(),
            request.VehicleTypeId,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.MaxGrossWeight,
            request.TareWeight,
            request.MaxUsableVolume,
            request.MaxPalletCount,
            request.MaxLoadHeight,
            request.CapacityPolicySnapshot);
        var record = ToRecord(capacity);
        dbContext.VehicleCapacities.Add(record);
        await auditWriter.AppendAsync(new(
            "VehicleCapacityCreated",
            nameof(VehicleCapacityRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.VehicleTypeId, record.EffectiveFrom, record.EffectiveTo })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<VehicleDto> CreateVehicleAsync(
        CreateVehicleRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"vehicle:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<VehicleDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var vehicleType = await dbContext.VehicleTypes.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == request.VehicleTypeId && x.IsActive,
            cancellationToken);
        if (vehicleType is null)
        {
            throw new DomainException(new("VEHICLE_TYPE_NOT_FOUND", "Aktif araç tipi bulunamadı."));
        }

        var vehicle = Vehicle.Create(
            Guid.NewGuid(),
            request.VehicleTypeId,
            request.PlateNumber,
            request.MaintenanceUntil,
            request.LastKnownLocationText);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var record = new VehicleRecord
        {
            Id = vehicle.Id,
            VehicleTypeId = vehicle.VehicleTypeId,
            PlateNumber = vehicle.PlateNumber,
            Status = vehicle.Status.ToString(),
            MaintenanceUntil = vehicle.MaintenanceUntil,
            CurrentRoutePlanId = vehicle.CurrentRoutePlanId,
            LastKnownLocationText = vehicle.LastKnownLocationText,
            LastStatusAt = now,
            RowVersion = 1,
        };
        dbContext.Vehicles.Add(record);
        await auditWriter.AppendAsync(new(
            "VehicleCreated",
            nameof(VehicleRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.VehicleTypeId, record.PlateNumber, record.Status })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<VehicleDto>> ListVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Vehicles
            .AsNoTracking()
            .OrderBy(x => x.PlateNumber)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<VehicleDto?> GetVehicleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<VehicleDto?> ChangeVehicleStatusAsync(
        Guid id,
        ChangeVehicleStatusRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"vehicle:status:{actorId}:{id}";
        var payloadHash = ComputePayloadHash(new { id, request, expectedRowVersion });
        var replay = await TryReplayAsync<VehicleDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await LockVehicleAsync(id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        EnsureExpectedVersion(record.RowVersion, expectedRowVersion, nameof(VehicleRecord), record.Id);
        if (!Enum.TryParse<VehicleStatus>(request.Status, true, out var status))
        {
            throw new DomainException(new("VEHICLE_STATUS_INVALID", "Geçersiz araç durumu."));
        }

        var vehicle = Vehicle.Rehydrate(
            record.Id,
            record.VehicleTypeId,
            record.PlateNumber,
            ParseEnum<VehicleStatus>(record.Status, "VEHICLE_STATUS_INVALID"),
            record.MaintenanceUntil,
            record.CurrentRoutePlanId,
            record.LastKnownLocationText,
            record.LastStatusAt);
        vehicle.ChangeStatus(status, DateTimeOffset.UtcNow);
        record.Status = vehicle.Status.ToString();
        record.LastStatusAt = vehicle.LastStatusAt;
        record.RowVersion++;
        await auditWriter.AppendAsync(new(
            "VehicleStatusChanged",
            nameof(VehicleRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.Status, request.Reason })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<DriverDto> CreateDriverAsync(
        CreateDriverRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"driver:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<DriverDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var driver = Driver.Create(
            Guid.NewGuid(),
            request.EmployeeId,
            request.FullName,
            request.Phone,
            request.LicenseNumber,
            request.LicenseExpiry);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = new DriverRecord
        {
            Id = driver.Id,
            EmployeeId = driver.EmployeeId,
            FullName = driver.FullName,
            Phone = driver.Phone,
            LicenseNumber = driver.LicenseNumber,
            LicenseExpiry = driver.LicenseExpiry,
            Status = driver.Status.ToString(),
            IsActive = true,
            RowVersion = 1,
        };
        dbContext.Drivers.Add(record);
        await auditWriter.AppendAsync(new(
            "DriverCreated",
            nameof(DriverRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.FullName, record.LicenseNumber, record.LicenseExpiry })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<DriverDto>> ListDriversAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Drivers
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<DriverDto?> GetDriverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Drivers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<DriverDto?> ChangeDriverStatusAsync(
        Guid id,
        ChangeDriverStatusRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"driver:status:{actorId}:{id}";
        var payloadHash = ComputePayloadHash(new { id, request, expectedRowVersion });
        var replay = await TryReplayAsync<DriverDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await LockDriverAsync(id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        EnsureExpectedVersion(record.RowVersion, expectedRowVersion, nameof(DriverRecord), record.Id);
        if (!Enum.TryParse<DriverStatus>(request.Status, true, out var status))
        {
            throw new DomainException(new("DRIVER_STATUS_INVALID", "Geçersiz şoför durumu."));
        }

        var driver = Driver.Rehydrate(
            record.Id,
            record.EmployeeId,
            record.FullName,
            record.Phone,
            record.LicenseNumber,
            record.LicenseExpiry,
            ParseEnum<DriverStatus>(record.Status, "DRIVER_STATUS_INVALID"));
        driver.ChangeStatus(status);
        record.Status = driver.Status.ToString();
        record.IsActive = driver.Status == DriverStatus.Active;
        record.RowVersion++;
        await auditWriter.AppendAsync(new(
            "DriverStatusChanged",
            nameof(DriverRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.Status, request.Reason })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<ShipmentDto> CreateShipmentAsync(
        CreateShipmentRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"shipment:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<ShipmentDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var deliveryNote = await dbContext.DeliveryNotes
            .FromSqlInterpolated($"SELECT * FROM delivery_notes WHERE id = {request.DeliveryNoteId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (deliveryNote is null)
        {
            throw new DomainException(new("DELIVERY_NOTE_NOT_FOUND", "İrsaliye bulunamadı."));
        }

        EnsureExpectedVersion(deliveryNote.RowVersion, request.ExpectedDeliveryNoteRowVersion, nameof(DeliveryNoteRecord), deliveryNote.Id);
        if (!string.Equals(deliveryNote.Status, "Issued", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(new("DELIVERY_NOTE_NOT_ISSUED", "Shipment yalnızca kesinleşmiş irsaliyeden oluşturulabilir."));
        }

        if (await dbContext.Shipments.AnyAsync(x => x.DeliveryNoteId == deliveryNote.Id, cancellationToken))
        {
            throw new DomainException(new("SHIPMENT_SOURCE_ALREADY_LINKED", "İrsaliye zaten bir shipment kaydına bağlıdır."));
        }

        var items = await dbContext.DeliveryNoteItems
            .AsNoTracking()
            .Where(x => x.DeliveryNoteId == deliveryNote.Id && x.ShippedQty > 0)
            .OrderBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        if (items.Length == 0)
        {
            throw new DomainException(new("SHIPMENT_ITEMS_REQUIRED", "Shipment için sevk edilmiş en az bir irsaliye kalemi gereklidir."));
        }

        var record = new ShipmentRecord
        {
            Id = Guid.NewGuid(),
            DeliveryNoteId = deliveryNote.Id,
            CustomerId = deliveryNote.CustomerId,
            Status = "Preparing",
            RowVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        foreach (var item in items)
        {
            record.Items.Add(new ShipmentItemRecord
            {
                Id = Guid.NewGuid(),
                ShipmentId = record.Id,
                DeliveryNoteItemId = item.Id,
                ProductId = item.ProductId,
                QuantityBase = item.ShippedQty,
                PackagingSnapshot = item.PackagingSnapshot,
            });
        }

        dbContext.Shipments.Add(record);
        await auditWriter.AppendAsync(new(
            "ShipmentCreated",
            nameof(ShipmentRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.DeliveryNoteId, record.Status, itemCount = record.Items.Count })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<ShipmentDto>> ListShipmentsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Shipments
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<ShipmentDto?> GetShipmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Shipments
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<RoutePlanDto> CreateRoutePlanAsync(
        Guid shipmentId,
        CreateRoutePlanRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"route-plan:create:{actorId}:{shipmentId}";
        var payloadHash = ComputePayloadHash(new { shipmentId, request });
        var replay = await TryReplayAsync<RoutePlanDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var shipment = await LockShipmentAsync(shipmentId, cancellationToken);
        if (shipment is null)
        {
            throw new DomainException(new("SHIPMENT_NOT_FOUND", "Shipment bulunamadı."));
        }

        EnsureExpectedVersion(shipment.RowVersion, request.ExpectedShipmentRowVersion, nameof(ShipmentRecord), shipment.Id);
        EnsureShipmentRouteable(shipment.Status);
        var version = (await dbContext.RoutePlans.Where(x => x.ShipmentId == shipmentId).MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1;
        var plan = RoutePlan.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, shipmentId, version, request.PlannedStartAt, request.PlannedEndAt);
        var record = ToRecord(plan);
        dbContext.RoutePlans.Add(record);
        await auditWriter.AppendAsync(new(
            "RoutePlanCreated",
            nameof(RoutePlanRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.ShipmentId, record.Version, record.Status })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<RoutePlanDto?> GetRoutePlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await LoadRouteAsync(id, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<RoutePlanDto?> ReplaceStopsAsync(
        Guid routePlanId,
        ReplaceRouteStopsRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"route-plan:stops:{actorId}:{routePlanId}";
        var payloadHash = ComputePayloadHash(new { routePlanId, request, expectedRowVersion });
        var replay = await TryReplayAsync<RoutePlanDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await LockRouteAsync(routePlanId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        EnsureExpectedVersion(record.RowVersion, expectedRowVersion, nameof(RoutePlanRecord), record.Id);
        var stops = request.Stops.OrderBy(x => x.SequenceNo).ToArray();
        var stopAggregates = stops.Select(x => RouteStop.Create(Guid.NewGuid(), x.SequenceNo, x.CustomerId, x.AddressId, x.PlannedArrivalAt)).ToArray();
        var plan = await RehydrateRouteAsync(record, cancellationToken);
        plan.ReplaceStops(stopAggregates);

        foreach (var stop in stops)
        {
            var addressOwned = await dbContext.CustomerAddresses.AnyAsync(
                x => x.Id == stop.AddressId && x.CustomerId == stop.CustomerId && x.IsActive,
                cancellationToken);
            if (!addressOwned)
            {
                throw new DomainException(new("ADDRESS_NOT_OWNED_BY_CUSTOMER", "Rota adresi seçilen müşteriye ait veya aktif değildir."));
            }
        }

        var existing = await dbContext.RouteStops.Where(x => x.RoutePlanId == record.Id).ToArrayAsync(cancellationToken);
        dbContext.RouteStops.RemoveRange(existing);
        foreach (var stop in plan.Stops)
        {
            dbContext.RouteStops.Add(new RouteStopRecord
            {
                Id = stop.Id,
                RoutePlanId = record.Id,
                SequenceNo = stop.SequenceNo,
                CustomerId = stop.CustomerId,
                AddressId = stop.AddressId,
                Status = stop.Status.ToString(),
                PlannedArrivalAt = stop.PlannedArrivalAt,
                RowVersion = 1,
            });
        }

        record.RowVersion++;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await auditWriter.AppendAsync(new(
            "RouteStopsReplaced",
            nameof(RoutePlanRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { stopCount = plan.Stops.Count })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = await MapRouteAsync(record.Id, cancellationToken);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<RoutePlanDto?> AssignResourcesAsync(
        Guid routePlanId,
        AssignRouteResourcesRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"route-plan:assign-resources:{actorId}:{routePlanId}";
        var payloadHash = ComputePayloadHash(new { routePlanId, request, expectedRowVersion });
        var replay = await TryReplayAsync<RoutePlanDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await LockRouteAsync(routePlanId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        EnsureExpectedVersion(record.RowVersion, expectedRowVersion, nameof(RoutePlanRecord), record.Id);
        var shipment = await LockShipmentAsync(record.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            throw new DomainException(new("SHIPMENT_NOT_FOUND", "Rota shipment kaydı bulunamadı."));
        }
        EnsureShipmentRouteable(shipment.Status);

        var resourceIds = new[] { record.VehicleId, request.VehicleId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().OrderBy(x => x).ToArray();
        var vehicles = new Dictionary<Guid, VehicleRecord>();
        foreach (var vehicleId in resourceIds)
        {
            var locked = await LockVehicleAsync(vehicleId, cancellationToken);
            if (locked is null)
            {
                throw new DomainException(new("VEHICLE_NOT_FOUND", "Araç bulunamadı."));
            }
            vehicles[vehicleId] = locked;
        }

        var driverIds = new[] { record.DriverId, request.DriverId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().OrderBy(x => x).ToArray();
        var drivers = new Dictionary<Guid, DriverRecord>();
        foreach (var driverId in driverIds)
        {
            var locked = await LockDriverAsync(driverId, cancellationToken);
            if (locked is null)
            {
                throw new DomainException(new("DRIVER_NOT_FOUND", "Şoför bulunamadı."));
            }
            drivers[driverId] = locked;
        }

        var now = DateTimeOffset.UtcNow;
        var candidateVehicle = vehicles[request.VehicleId];
        var candidateDriver = drivers[request.DriverId];
        EnsureVehicleAssignable(candidateVehicle, record.Id, record.PlannedStartAt, record.PlannedEndAt, now);
        EnsureDriverAssignable(candidateDriver, record.PlannedStartAt, record.PlannedEndAt);
        await EnsureNoScheduleConflictAsync(request.VehicleId, request.DriverId, record, cancellationToken);

        var plan = await RehydrateRouteAsync(record, cancellationToken);
        plan.AssignResources(request.VehicleId, request.DriverId);
        record.VehicleId = plan.VehicleId;
        record.DriverId = plan.DriverId;
        record.RowVersion++;
        record.UpdatedAt = now;

        if (record.VehicleId is not null)
        {
            candidateVehicle.CurrentRoutePlanId = record.Id;
            candidateVehicle.Status = VehicleStatus.Assigned.ToString();
            candidateVehicle.LastStatusAt = now;
            candidateVehicle.RowVersion++;
        }

        await auditWriter.AppendAsync(new(
            "RouteResourcesAssigned",
            nameof(RoutePlanRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.VehicleId, record.DriverId })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = await MapRouteAsync(record.Id, cancellationToken);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task<RoutePlanDto?> PlanRouteAsync(
        Guid routePlanId,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
        => TransitionRouteAsync(
            routePlanId,
            expectedRowVersion,
            actorId,
            idempotencyKey,
            correlationId,
            "plan",
            static plan => plan.Plan(),
            "RoutePlanned",
            cancellationToken);

    public Task<RoutePlanDto?> LockRouteAsync(
        Guid routePlanId,
        RouteConfirmationRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!request.Confirmation)
        {
            throw new DomainException(new("CONFIRMATION_REQUIRED", "Rota kilitleme için confirmation=true gönderilmelidir."));
        }

        return TransitionRouteAsync(
            routePlanId,
            expectedRowVersion,
            actorId,
            idempotencyKey,
            correlationId,
            "lock",
            static plan => plan.Lock(),
            "RouteLocked",
            cancellationToken);
    }

    public async Task<RoutePlanDto?> ReplanRouteAsync(
        Guid routePlanId,
        ReplanRouteRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"route-plan:replan:{actorId}:{routePlanId}";
        var payloadHash = ComputePayloadHash(new { routePlanId, request, expectedRowVersion });
        var replay = await TryReplayAsync<RoutePlanDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var oldRecord = await LockRouteAsync(routePlanId, cancellationToken);
        if (oldRecord is null)
        {
            return null;
        }

        EnsureExpectedVersion(oldRecord.RowVersion, expectedRowVersion, nameof(RoutePlanRecord), oldRecord.Id);
        if (!string.Equals(oldRecord.Status, nameof(RoutePlanStatus.Locked), StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(new("ROUTE_STATE_CONFLICT", "Yalnızca kilitli rota yeniden planlanabilir."));
        }
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new DomainException(new("REPLAN_REASON_REQUIRED", "Replan gerekçesi zorunludur."));
        }

        var shipment = await LockShipmentAsync(oldRecord.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            throw new DomainException(new("SHIPMENT_NOT_FOUND", "Rota shipment kaydı bulunamadı."));
        }
        var version = (await dbContext.RoutePlans.Where(x => x.ShipmentId == oldRecord.ShipmentId).MaxAsync(x => (int?)x.Version, cancellationToken) ?? oldRecord.Version) + 1;
        var newPlan = RoutePlan.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, oldRecord.ShipmentId, version, request.PlannedStartAt, request.PlannedEndAt);
        var oldStops = await dbContext.RouteStops.AsNoTracking().Where(x => x.RoutePlanId == oldRecord.Id).OrderBy(x => x.SequenceNo).ToArrayAsync(cancellationToken);
        newPlan.ReplaceStops(oldStops.Select(x => RouteStop.Create(Guid.NewGuid(), x.SequenceNo, x.CustomerId, x.AddressId, x.PlannedArrivalAt)));
        var record = ToRecord(newPlan);
        record.ReplannedFromId = oldRecord.Id;
        dbContext.RoutePlans.Add(record);
        foreach (var stop in newPlan.Stops)
        {
            dbContext.RouteStops.Add(new RouteStopRecord
            {
                Id = stop.Id,
                RoutePlanId = record.Id,
                SequenceNo = stop.SequenceNo,
                CustomerId = stop.CustomerId,
                AddressId = stop.AddressId,
                Status = stop.Status.ToString(),
                PlannedArrivalAt = stop.PlannedArrivalAt,
                RowVersion = 1,
            });
        }

        await auditWriter.AppendAsync(new(
            "RouteReplanned",
            nameof(RoutePlanRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.ReplannedFromId, record.Version, request.Reason })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = await MapRouteAsync(record.Id, cancellationToken);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<RoutePlanDto?> TransitionRouteAsync(
        Guid routePlanId,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        string action,
        Action<RoutePlan> transition,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var scope = $"route-plan:{action}:{actorId}:{routePlanId}";
        var payloadHash = ComputePayloadHash(new { routePlanId, action, expectedRowVersion });
        var replay = await TryReplayAsync<RoutePlanDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await LockRouteAsync(routePlanId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        EnsureExpectedVersion(record.RowVersion, expectedRowVersion, nameof(RoutePlanRecord), record.Id);
        var shipment = await LockShipmentAsync(record.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            throw new DomainException(new("SHIPMENT_NOT_FOUND", "Rota shipment kaydı bulunamadı."));
        }
        EnsureShipmentRouteable(shipment.Status);
        var plan = await RehydrateRouteAsync(record, cancellationToken);
        if (plan.VehicleId is null || plan.DriverId is null)
        {
            throw new DomainException(new("ROUTE_RESOURCES_REQUIRED", "Rota geçişi için araç ve şoför atanmalıdır."));
        }

        var vehicle = await LockVehicleAsync(plan.VehicleId.Value, cancellationToken);
        var driver = await LockDriverAsync(plan.DriverId.Value, cancellationToken);
        if (vehicle is null || driver is null)
        {
            throw new DomainException(new("ROUTE_RESOURCES_NOT_FOUND", "Rota araç veya şoför kaydı bulunamadı."));
        }
        EnsureVehicleAssignable(vehicle, record.Id, record.PlannedStartAt, record.PlannedEndAt, DateTimeOffset.UtcNow);
        EnsureDriverAssignable(driver, record.PlannedStartAt, record.PlannedEndAt);
        await EnsureNoScheduleConflictAsync(plan.VehicleId.Value, plan.DriverId.Value, record, cancellationToken);

        transition(plan);
        record.Status = plan.Status.ToString();
        record.RowVersion++;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await auditWriter.AppendAsync(new(
            auditAction,
            nameof(RoutePlanRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.Status, record.Version })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = await MapRouteAsync(record.Id, cancellationToken);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task EnsureNoScheduleConflictAsync(
        Guid vehicleId,
        Guid driverId,
        RoutePlanRecord record,
        CancellationToken cancellationToken)
    {
        if (record.PlannedStartAt is null || record.PlannedEndAt is null)
        {
            return;
        }

        var vehicleConflict = await dbContext.RoutePlans.AsNoTracking().AnyAsync(
            x => x.Id != record.Id
                && x.VehicleId == vehicleId
                && ActiveRouteStatuses.Contains(x.Status)
                && x.PlannedStartAt != null
                && x.PlannedEndAt != null
                && x.PlannedStartAt < record.PlannedEndAt
                && x.PlannedEndAt > record.PlannedStartAt,
            cancellationToken);
        if (vehicleConflict)
        {
            throw new DomainException(new(
                "VEHICLE_SCHEDULE_CONFLICT",
                "Araç seçilen zaman aralığında başka bir aktif rota ile çakışıyor."));
        }

        var driverConflict = await dbContext.RoutePlans.AsNoTracking().AnyAsync(
            x => x.Id != record.Id
                && x.DriverId == driverId
                && ActiveRouteStatuses.Contains(x.Status)
                && x.PlannedStartAt != null
                && x.PlannedEndAt != null
                && x.PlannedStartAt < record.PlannedEndAt
                && x.PlannedEndAt > record.PlannedStartAt,
            cancellationToken);
        if (driverConflict)
        {
            throw new DomainException(new(
                "DRIVER_SCHEDULE_CONFLICT",
                "Şoför seçilen zaman aralığında başka bir aktif rota ile çakışıyor."));
        }
    }

    private static void EnsureVehicleAssignable(
        VehicleRecord record,
        Guid routePlanId,
        DateTimeOffset? start,
        DateTimeOffset? end,
        DateTimeOffset now)
    {
        if (record.Status is "Maintenance" or "OutOfService" or "InTransit")
        {
            throw new DomainException(new("VEHICLE_UNAVAILABLE", "Araç mevcut durumu nedeniyle atanamaz."));
        }

        if (record.MaintenanceUntil is not null && start is not null && record.MaintenanceUntil > start)
        {
            throw new DomainException(new("VEHICLE_MAINTENANCE", "Araç rota başlangıcında bakımda olacaktır."));
        }

    }

    private static void EnsureDriverAssignable(DriverRecord record, DateTimeOffset? start, DateTimeOffset? end)
    {
        if (!record.IsActive || !string.Equals(record.Status, nameof(DriverStatus.Active), StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(new("DRIVER_INACTIVE", "Şoför aktif olmadığı için atanamaz."));
        }

        if (end is not null && record.LicenseExpiry < DateOnly.FromDateTime(end.Value.UtcDateTime.Date))
        {
            throw new DomainException(new("DRIVER_LICENSE_EXPIRED", "Ehliyet rota bitiş tarihine kadar geçerli değildir."));
        }
    }

    private static void EnsureShipmentRouteable(string status)
    {
        if (!string.Equals(status, "Preparing", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(new("SHIPMENT_NOT_ROUTEABLE", "Shipment mevcut durumunda rotalanamaz."));
        }
    }

    private static void EnsureExpectedVersion(long actual, long expected, string resourceType, Guid resourceId)
    {
        if (actual != expected)
        {
            throw new DomainException(new(
                "RESOURCE_VERSION_CONFLICT",
                "Kayıt başka bir işlem tarafından değiştirildi.",
                new Dictionary<string, object?>
                {
                    ["resourceType"] = resourceType,
                    ["resourceId"] = resourceId,
                    ["currentRowVersion"] = actual,
                    ["expectedRowVersion"] = expected,
                }));
        }
    }

    private async Task<VehicleTypeRecord?> LockVehicleTypeAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.VehicleTypes
            .FromSqlInterpolated($"SELECT * FROM vehicle_types WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<VehicleRecord?> LockVehicleAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Vehicles
            .FromSqlInterpolated($"SELECT * FROM vehicles WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<DriverRecord?> LockDriverAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Drivers
            .FromSqlInterpolated($"SELECT * FROM drivers WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<ShipmentRecord?> LockShipmentAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Shipments
            .FromSqlInterpolated($"SELECT * FROM shipments WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<RoutePlanRecord?> LockRouteAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.RoutePlans
            .FromSqlInterpolated($"SELECT * FROM route_plans WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<RoutePlanRecord?> LoadRouteAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.RoutePlans.AsNoTracking().Include(x => x.Stops).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    private async Task<RoutePlan> RehydrateRouteAsync(RoutePlanRecord record, CancellationToken cancellationToken)
    {
        var stops = await dbContext.RouteStops.AsNoTracking().Where(x => x.RoutePlanId == record.Id).OrderBy(x => x.SequenceNo).ToArrayAsync(cancellationToken);
        return RoutePlan.Rehydrate(
            record.Id,
            record.CreatedAt,
            record.ShipmentId,
            record.Version,
            record.ReplannedFromId,
            record.VehicleId,
            record.DriverId,
            ParseEnum<RoutePlanStatus>(record.Status, "ROUTE_STATE_INVALID"),
            record.PlannedStartAt,
            record.PlannedEndAt,
            stops.Select(x => RouteStop.Rehydrate(
                x.Id,
                x.SequenceNo,
                x.CustomerId,
                x.AddressId,
                ParseEnum<RouteStopStatus>(x.Status, "ROUTE_STOP_STATE_INVALID"),
                x.PlannedArrivalAt)));
    }

    private async Task<RoutePlanDto> MapRouteAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await LoadRouteAsync(id, cancellationToken)
            ?? throw new DomainException(new("ROUTE_PLAN_NOT_FOUND", "Rota planı bulunamadı."));
        return Map(record);
    }

    private static ShipmentDto Map(ShipmentRecord record)
        => new(
            record.Id,
            record.DeliveryNoteId,
            record.CustomerId,
            record.Status,
            record.Items.OrderBy(x => x.Id).Select(x => new ShipmentItemDto(
                x.Id,
                x.DeliveryNoteItemId,
                x.ProductId,
                x.QuantityBase,
                x.PackagingSnapshot)).ToArray(),
            record.RowVersion,
            record.CreatedAt);

    private static VehicleTypeDto Map(VehicleTypeRecord record)
        => new(record.Id, record.Code, record.Name, record.IsActive);

    private static VehicleCapacityDto Map(VehicleCapacityRecord record)
        => new(
            record.Id,
            record.VehicleTypeId,
            record.EffectiveFrom,
            record.EffectiveTo,
            record.MaxGrossWeight,
            record.TareWeight,
            record.MaxGrossWeight - record.TareWeight,
            record.MaxUsableVolume,
            record.MaxPalletCount,
            record.MaxLoadHeight,
            record.RowVersion);

    private static VehicleDto Map(VehicleRecord record)
        => new(
            record.Id,
            record.VehicleTypeId,
            record.PlateNumber,
            record.Status,
            record.MaintenanceUntil,
            record.CurrentRoutePlanId,
            record.LastKnownLocationText,
            record.LastStatusAt,
            record.RowVersion);

    private static DriverDto Map(DriverRecord record)
        => new(
            record.Id,
            record.EmployeeId,
            record.FullName,
            record.Phone,
            record.LicenseNumber,
            record.LicenseExpiry,
            record.Status,
            record.IsActive,
            record.RowVersion);

    private static RoutePlanDto Map(RoutePlanRecord record)
        => new(
            record.Id,
            record.ShipmentId,
            record.VehicleId,
            record.DriverId,
            record.Status,
            record.Version,
            record.ReplannedFromId,
            record.PlannedStartAt,
            record.PlannedEndAt,
            record.Stops.OrderBy(x => x.SequenceNo).Select(x => new RouteStopDto(
                x.Id,
                x.SequenceNo,
                x.CustomerId,
                x.AddressId,
                x.Status,
                x.PlannedArrivalAt,
                x.RowVersion)).ToArray(),
            record.RowVersion,
            record.CreatedAt,
            record.UpdatedAt);

    private static VehicleCapacityRecord ToRecord(VehicleCapacity capacity)
        => new()
        {
            Id = capacity.Id,
            VehicleTypeId = capacity.VehicleTypeId,
            EffectiveFrom = capacity.EffectiveFrom,
            EffectiveTo = capacity.EffectiveTo,
            MaxGrossWeight = capacity.MaxGrossWeight,
            TareWeight = capacity.TareWeight,
            MaxUsableVolume = capacity.MaxUsableVolume,
            MaxPalletCount = capacity.MaxPalletCount,
            MaxLoadHeight = capacity.MaxLoadHeight,
            CapacityPolicySnapshot = capacity.PolicySnapshot,
            RowVersion = 1,
        };

    private static RoutePlanRecord ToRecord(RoutePlan plan)
        => new()
        {
            Id = plan.Id,
            ShipmentId = plan.ShipmentId,
            VehicleId = plan.VehicleId,
            DriverId = plan.DriverId,
            Status = plan.Status.ToString(),
            Version = plan.Version,
            ReplannedFromId = plan.ReplannedFromId,
            PlannedStartAt = plan.PlannedStartAt,
            PlannedEndAt = plan.PlannedEndAt,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.CreatedAt,
            RowVersion = 1,
        };

    private static TEnum ParseEnum<TEnum>(string value, string code)
        where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, true, out var result)
            ? result
            : throw new DomainException(new(code, $"Geçersiz state değeri: {value}."));

    private async Task<T?> TryReplayAsync<T>(
        string scope,
        string key,
        string payloadHash,
        CancellationToken cancellationToken)
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

    private Task SaveIdempotencyAsync<T>(
        string scope,
        string key,
        int statusCode,
        T result,
        CancellationToken cancellationToken)
        => SaveIdempotencyAsync(scope, key, ComputePayloadHash(result), statusCode, result, cancellationToken);

    private async Task SaveIdempotencyAsync<T>(
        string scope,
        string key,
        string payloadHash,
        int statusCode,
        T result,
        CancellationToken cancellationToken)
        => await idempotencyStore.SaveAsync(
            scope,
            key,
            payloadHash,
            statusCode,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);

    private static string ComputePayloadHash<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }
}
