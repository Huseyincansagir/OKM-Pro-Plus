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

public sealed class LoadVerificationCommandService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : ILoadVerificationCommandService
{
    public async Task<LoadVerificationSessionDto> StartSessionAsync(
        Guid loadPlanId,
        StartLoadVerificationRequest request,
        long expectedLoadPlanRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "LoadPlan zorunludur.");
        var scope = $"load-verification:start:{actorId}:{loadPlanId}";
        var payloadHash = ComputePayloadHash(new { loadPlanId, request });
        var replay = await TryReplayAsync<LoadVerificationSessionDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var plan = await LockLoadPlanAsync(loadPlanId, cancellationToken)
            ?? throw new DomainException(new("LOAD_PLAN_NOT_FOUND", "LoadPlan bulunamadı."));
        EnsureExpectedVersion(plan.RowVersion, expectedLoadPlanRowVersion, nameof(LoadPlanRecord), plan.Id);
        var shipment = await LockShipmentAsync(plan.ShipmentId, cancellationToken)
            ?? throw new DomainException(new("SHIPMENT_NOT_FOUND", "LoadPlan shipment kaydı bulunamadı."));

        var activeSession = await dbContext.LoadVerificationSessions
            .FromSqlInterpolated($"SELECT * FROM load_verification_sessions WHERE load_plan_id = {loadPlanId} AND status IN ('Draft', 'InProgress') FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        LoadVerificationSession.EnsureStartAllowed(ParseEnum<LoadPlanStatus>(plan.Status, "LOAD_PLAN_STATUS_INVALID"), activeSession is null ? null : ParseEnum<LoadVerificationSessionStatus>(activeSession.Status, "LOAD_VERIFICATION_STATUS_INVALID"));
        if (shipment.Status is "Loaded" or "InTransit" or "Delivered" or "PartiallyDelivered")
        {
            throw new DomainException(new("SHIPMENT_ALREADY_LOADED", "Shipment mevcut state’i yeni load verification başlatamaz."));
        }

        var now = DateTimeOffset.UtcNow;
        var session = LoadVerificationSession.Create(Guid.NewGuid(), now, plan.Id, shipment.Id, actorId);
        session.Start(now);
        var record = new LoadVerificationSessionRecord
        {
            Id = session.Id,
            LoadPlanId = session.LoadPlanId,
            ShipmentId = session.ShipmentId,
            Status = session.Status.ToString(),
            StartedBy = session.StartedBy,
            StartedAt = session.StartedAt,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            RowVersion = session.RowVersion,
        };
        dbContext.LoadVerificationSessions.Add(record);
        await auditWriter.AppendAsync(new(
            "LoadVerificationSessionStarted",
            nameof(LoadVerificationSessionRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.LoadPlanId, record.ShipmentId, record.Status })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record, Array.Empty<LoadVerificationScanRecord>());
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<LoadVerificationSessionDto?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.LoadVerificationSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var scans = await dbContext.LoadVerificationScans
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.ScannedAt)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        return Map(record, scans);
    }

    public async Task<LoadVerificationScanDto> ScanAsync(
        Guid sessionId,
        ScanLoadVerificationRequest request,
        long expectedSessionRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(sessionId, "LOAD_VERIFICATION_SESSION_REQUIRED", "Load verification session zorunludur.");
        DomainGuard.AgainstBlank(request.Barcode, "PACKAGE_BARCODE_REQUIRED", "ShipmentPackage barkodu zorunludur.");
        var barcode = request.Barcode.Trim();
        var scanMode = ParseEnum<LoadVerificationScanMode>(request.ScanMode, "LOAD_VERIFICATION_SCAN_MODE_INVALID");
        var scope = $"load-verification:scan:{actorId}:{sessionId}";
        var payloadHash = ComputePayloadHash(new { sessionId, request = request with { Barcode = barcode, ScanMode = scanMode.ToString() } });
        var replay = await TryReplayAsync<LoadVerificationScanDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var sessionReference = await dbContext.LoadVerificationSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken)
            ?? throw new DomainException(new("LOAD_VERIFICATION_SESSION_NOT_FOUND", "Load verification session bulunamadı."));
        var plan = await LockLoadPlanAsync(sessionReference.LoadPlanId, cancellationToken)
            ?? throw new DomainException(new("LOAD_PLAN_NOT_FOUND", "LoadPlan bulunamadı."));
        var shipment = await LockShipmentAsync(plan.ShipmentId, cancellationToken)
            ?? throw new DomainException(new("SHIPMENT_NOT_FOUND", "LoadPlan shipment kaydı bulunamadı."));
        var session = await LockSessionForUpdateAsync(sessionId, cancellationToken)
            ?? throw new DomainException(new("LOAD_VERIFICATION_SESSION_NOT_FOUND", "Load verification session bulunamadı."));
        EnsureExpectedVersion(session.RowVersion, expectedSessionRowVersion, nameof(LoadVerificationSessionRecord), session.Id);
        if (session.ShipmentId != shipment.Id || plan.ShipmentId != session.ShipmentId)
        {
            throw new DomainException(new("LOAD_VERIFICATION_OWNERSHIP_CONFLICT", "Session, LoadPlan ve Shipment ownership zinciri uyuşmuyor."));
        }

        if (session.Status == nameof(LoadVerificationSessionStatus.Completed))
        {
            throw new DomainException(new("LOAD_VERIFICATION_COMPLETED", "Tamamlanmış session’a yeni scan eklenemez."));
        }
        if (session.Status != nameof(LoadVerificationSessionStatus.InProgress))
        {
            throw new DomainException(new("LOAD_VERIFICATION_INVALID_STATE", "Scan için session InProgress olmalıdır."));
        }
        if (plan.Status != nameof(LoadPlanStatus.Locked))
        {
            throw new DomainException(new("LOAD_PLAN_NOT_LOCKED", "Yükleme doğrulaması yalnızca Locked LoadPlan üzerinde yapılabilir."));
        }

        var package = await dbContext.ShipmentPackages
            .FromSqlInterpolated($"SELECT * FROM shipment_packages WHERE shipment_id = {shipment.Id} AND package_code = {barcode} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        await LockLoadUnitsAndItemsAsync(plan.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        LoadUnitItemRecord? allocation = null;
        if (package is not null)
        {
            allocation = await dbContext.LoadUnitItems
                .Where(x => x.ShipmentPackageId == package.Id && x.LoadUnit.LoadPlanId == plan.Id)
                .OrderBy(x => x.LoadUnit.UnitCode)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var status = LoadVerificationScanStatus.Accepted;
        string? reasonCode = null;
        string? reasonText = null;
        Guid? expectedLoadUnitId = allocation?.LoadUnitId;
        Guid? actualLoadUnitId = request.ExpectedLoadUnitId;
        var quantityBase = package?.QuantityBase ?? 1m;

        if (package is null)
        {
            status = LoadVerificationScanStatus.Unexpected;
            reasonCode = "PACKAGE_BARCODE_NOT_FOUND";
            reasonText = "Barkod aynı shipment içindeki aktif veya iptal paketlerde bulunamadı.";
        }
        else if (package.Status == nameof(ShipmentPackageStatus.Cancelled))
        {
            status = LoadVerificationScanStatus.CancelledPackage;
            reasonCode = "PACKAGE_CANCELLED";
            reasonText = "Cancelled ShipmentPackage yüklenemez.";
        }
        else if (package.Status == nameof(ShipmentPackageStatus.Loaded))
        {
            status = LoadVerificationScanStatus.Duplicate;
            reasonCode = "PACKAGE_ALREADY_LOADED";
            reasonText = "ShipmentPackage daha önce Loaded durumuna geçmiştir.";
        }
        else if (allocation is null)
        {
            status = LoadVerificationScanStatus.Unexpected;
            reasonCode = "PACKAGE_NOT_IN_LOAD_PLAN";
            reasonText = "ShipmentPackage locked LoadPlan içinde allocation’a sahip değildir.";
        }
        else if (request.ExpectedLoadUnitId is not null && request.ExpectedLoadUnitId != allocation.LoadUnitId)
        {
            status = LoadVerificationScanStatus.WrongUnit;
            reasonCode = "LOAD_UNIT_MISMATCH";
            reasonText = "Okutulan paket beklenen LoadUnit ile eşleşmemektedir.";
        }
        else
        {
            LoadVerificationPolicy.EnsurePackageCanBeAccepted(
                ParseEnum<LoadPlanStatus>(plan.Status, "LOAD_PLAN_STATUS_INVALID"),
                ParseEnum<LoadVerificationSessionStatus>(session.Status, "LOAD_VERIFICATION_STATUS_INVALID"),
                ParseEnum<ShipmentPackageStatus>(package.Status, "SHIPMENT_PACKAGE_STATUS_INVALID"),
                true,
                allocation.LoadUnitId,
                request.ExpectedLoadUnitId ?? allocation.LoadUnitId);

            var domainPackage = RehydratePackage(package);
            domainPackage.Load();
            package.Status = nameof(ShipmentPackageStatus.Loaded);
            package.UpdatedAt = now;
            package.RowVersion++;
            if (request.ExpectedLoadUnitId is not null)
            {
                actualLoadUnitId = request.ExpectedLoadUnitId;
            }
            await MarkCompletedLoadUnitsAsync(plan.Id, session.Id, now, cancellationToken);
        }

        var scan = LoadVerificationScan.Create(
            Guid.NewGuid(),
            now,
            session.Id,
            plan.Id,
            shipment.Id,
            package?.Id,
            expectedLoadUnitId,
            actualLoadUnitId,
            barcode,
            status,
            scanMode,
            quantityBase,
            reasonCode,
            reasonText,
            actorId,
            idempotencyKey,
            correlationId);
        var scanRecord = ToRecord(scan);
        dbContext.LoadVerificationScans.Add(scanRecord);
        session.UpdatedAt = now;
        session.RowVersion++;
        await auditWriter.AppendAsync(new(
            "LoadVerificationScanRecorded",
            nameof(LoadVerificationScanRecord),
            scanRecord.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                scanRecord.SessionId,
                scanRecord.ShipmentPackageId,
                scanRecord.Status,
                scanRecord.ReasonCode,
                scanRecord.ExpectedLoadUnitId,
                scanRecord.ActualLoadUnitId,
            })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(scanRecord);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<LoadVerificationSessionDto> CompleteAsync(
        Guid sessionId,
        CompleteLoadVerificationRequest request,
        long expectedSessionRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(sessionId, "LOAD_VERIFICATION_SESSION_REQUIRED", "Load verification session zorunludur.");
        var scope = $"load-verification:complete:{actorId}:{sessionId}";
        var payloadHash = ComputePayloadHash(new { sessionId, request });
        var replay = await TryReplayAsync<LoadVerificationSessionDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var sessionReference = await dbContext.LoadVerificationSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken)
            ?? throw new DomainException(new("LOAD_VERIFICATION_SESSION_NOT_FOUND", "Load verification session bulunamadı."));
        var plan = await LockLoadPlanAsync(sessionReference.LoadPlanId, cancellationToken)
            ?? throw new DomainException(new("LOAD_PLAN_NOT_FOUND", "LoadPlan bulunamadı."));
        var shipment = await LockShipmentAsync(plan.ShipmentId, cancellationToken)
            ?? throw new DomainException(new("SHIPMENT_NOT_FOUND", "Shipment bulunamadı."));
        var session = await LockSessionForUpdateAsync(sessionId, cancellationToken)
            ?? throw new DomainException(new("LOAD_VERIFICATION_SESSION_NOT_FOUND", "Load verification session bulunamadı."));
        EnsureExpectedVersion(session.RowVersion, expectedSessionRowVersion, nameof(LoadVerificationSessionRecord), session.Id);
        await LockLoadUnitsAndItemsAsync(plan.Id, cancellationToken);

        if (session.Status == nameof(LoadVerificationSessionStatus.Completed))
        {
            throw new DomainException(new("LOAD_VERIFICATION_COMPLETED", "Load verification session zaten tamamlanmıştır."));
        }
        var expectedPackageIds = await GetExpectedPackageIdsAsync(plan.Id, cancellationToken);
        var acceptedPackageIds = await GetAcceptedPackageIdsAsync(session.Id, cancellationToken);
        LoadVerificationPolicy.EnsureCompletionAllowed(
            ParseEnum<LoadVerificationSessionStatus>(session.Status, "LOAD_VERIFICATION_STATUS_INVALID"),
            expectedPackageIds.All(acceptedPackageIds.Contains));

        var domainSession = LoadVerificationSession.Create(session.Id, session.CreatedAt, session.LoadPlanId, session.ShipmentId, session.StartedBy);
        domainSession.Start(session.StartedAt);
        domainSession.Complete(actorId, true, DateTimeOffset.UtcNow);
        session.Status = domainSession.Status.ToString();
        session.CompletedBy = domainSession.CompletedBy;
        session.CompletedAt = domainSession.CompletedAt;
        session.CompletionReason = domainSession.CompletionReason;
        session.UpdatedAt = domainSession.UpdatedAt;
        session.RowVersion++;
        foreach (var unit in await dbContext.LoadUnits.Where(x => x.LoadPlanId == plan.Id).ToArrayAsync(cancellationToken))
        {
            var unitPackageIds = await dbContext.LoadUnitItems
                .Where(x => x.LoadUnitId == unit.Id)
                .Select(x => x.ShipmentPackageId)
                .ToArrayAsync(cancellationToken);
            if (unitPackageIds.Length > 0 && unitPackageIds.All(acceptedPackageIds.Contains))
            {
                unit.Status = nameof(LoadUnitStatus.Loaded);
                unit.UpdatedAt = DateTimeOffset.UtcNow;
                unit.RowVersion++;
            }
        }

        if (shipment.Status is not ("Preparing" or "Loaded"))
        {
            throw new DomainException(new("SHIPMENT_INVALID_LOAD_TRANSITION", $"{shipment.Status} durumundaki Shipment Loaded durumuna geçemez."));
        }
        shipment.Status = "Loaded";
        shipment.RowVersion++;
        await auditWriter.AppendAsync(new(
            "LoadVerificationCompleted",
            nameof(LoadVerificationSessionRecord),
            session.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { sessionStatus = session.Status, shipmentStatus = shipment.Status, acceptedPackageCount = acceptedPackageIds.Count })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var scans = await dbContext.LoadVerificationScans
            .Where(x => x.SessionId == session.Id)
            .OrderBy(x => x.ScannedAt)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var result = Map(session, scans);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<LoadVerificationSessionDto> CloseDiscrepancyAsync(
        Guid sessionId,
        CloseLoadVerificationDiscrepancyRequest request,
        long expectedSessionRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(sessionId, "LOAD_VERIFICATION_SESSION_REQUIRED", "Load verification session zorunludur.");
        DomainGuard.AgainstBlank(request.Reason, "LOAD_VERIFICATION_DISCREPANCY_REASON_REQUIRED", "Discrepancy kapanış gerekçesi zorunludur.");
        var scope = $"load-verification:discrepancy:{actorId}:{sessionId}";
        var payloadHash = ComputePayloadHash(new { sessionId, request });
        var replay = await TryReplayAsync<LoadVerificationSessionDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var sessionReference = await dbContext.LoadVerificationSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken)
            ?? throw new DomainException(new("LOAD_VERIFICATION_SESSION_NOT_FOUND", "Load verification session bulunamadı."));
        var plan = await LockLoadPlanAsync(sessionReference.LoadPlanId, cancellationToken)
            ?? throw new DomainException(new("LOAD_PLAN_NOT_FOUND", "LoadPlan bulunamadı."));
        _ = await LockShipmentAsync(plan.ShipmentId, cancellationToken)
            ?? throw new DomainException(new("SHIPMENT_NOT_FOUND", "Shipment bulunamadı."));
        var session = await LockSessionForUpdateAsync(sessionId, cancellationToken)
            ?? throw new DomainException(new("LOAD_VERIFICATION_SESSION_NOT_FOUND", "Load verification session bulunamadı."));
        EnsureExpectedVersion(session.RowVersion, expectedSessionRowVersion, nameof(LoadVerificationSessionRecord), session.Id);
        if (session.Status == nameof(LoadVerificationSessionStatus.Completed))
        {
            throw new DomainException(new("LOAD_VERIFICATION_COMPLETED", "Tamamlanmış session discrepancy olarak kapatılamaz."));
        }

        var domainSession = LoadVerificationSession.Create(session.Id, session.CreatedAt, session.LoadPlanId, session.ShipmentId, session.StartedBy);
        domainSession.Start(session.StartedAt);
        domainSession.CloseAsDiscrepancy(actorId, request.Reason, DateTimeOffset.UtcNow);
        session.Status = domainSession.Status.ToString();
        session.CompletedBy = domainSession.CompletedBy;
        session.CompletedAt = domainSession.CompletedAt;
        session.CompletionReason = domainSession.CompletionReason;
        session.UpdatedAt = domainSession.UpdatedAt;
        session.RowVersion++;
        await auditWriter.AppendAsync(new(
            "LoadVerificationDiscrepancyClosed",
            nameof(LoadVerificationSessionRecord),
            session.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { session.Status, session.CompletionReason })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var scans = await dbContext.LoadVerificationScans
            .Where(x => x.SessionId == session.Id)
            .OrderBy(x => x.ScannedAt)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var result = Map(session, scans);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<LoadPlanRecord?> LockLoadPlanAsync(Guid loadPlanId, CancellationToken cancellationToken)
        => await dbContext.LoadPlans
            .FromSqlInterpolated($"SELECT * FROM load_plans WHERE id = {loadPlanId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<ShipmentRecord?> LockShipmentAsync(Guid shipmentId, CancellationToken cancellationToken)
        => await dbContext.Shipments
            .FromSqlInterpolated($"SELECT * FROM shipments WHERE id = {shipmentId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<LoadVerificationSessionRecord?> LockSessionForUpdateAsync(Guid sessionId, CancellationToken cancellationToken)
        => await dbContext.LoadVerificationSessions
            .FromSqlInterpolated($"SELECT * FROM load_verification_sessions WHERE id = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task LockLoadUnitsAndItemsAsync(Guid loadPlanId, CancellationToken cancellationToken)
    {
        await dbContext.LoadUnits
            .FromSqlInterpolated($"SELECT * FROM load_units WHERE load_plan_id = {loadPlanId} ORDER BY unit_code, id FOR UPDATE")
            .ToListAsync(cancellationToken);
        await dbContext.LoadUnitItems
            .FromSqlInterpolated($"SELECT lui.* FROM load_unit_items lui INNER JOIN load_units lu ON lu.id = lui.load_unit_id WHERE lu.load_plan_id = {loadPlanId} ORDER BY lu.unit_code, lui.id FOR UPDATE")
            .ToListAsync(cancellationToken);
    }

    private async Task MarkCompletedLoadUnitsAsync(Guid loadPlanId, Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var acceptedPackageIds = await GetAcceptedPackageIdsAsync(sessionId, cancellationToken);
        var units = await dbContext.LoadUnits.Where(x => x.LoadPlanId == loadPlanId).ToArrayAsync(cancellationToken);
        foreach (var unit in units)
        {
            var packageIds = await dbContext.LoadUnitItems
                .Where(x => x.LoadUnitId == unit.Id)
                .Select(x => x.ShipmentPackageId)
                .ToArrayAsync(cancellationToken);
            if (packageIds.Length > 0 && packageIds.All(acceptedPackageIds.Contains))
            {
                unit.Status = nameof(LoadUnitStatus.Loaded);
                unit.UpdatedAt = now;
                unit.RowVersion++;
            }
        }
    }

    private Task<Guid[]> GetExpectedPackageIdsAsync(Guid loadPlanId, CancellationToken cancellationToken)
        => dbContext.LoadUnitItems
            .Where(x => x.LoadUnit.LoadPlanId == loadPlanId)
            .Select(x => x.ShipmentPackageId)
            .Distinct()
            .OrderBy(x => x)
            .ToArrayAsync(cancellationToken);

    private async Task<HashSet<Guid>> GetAcceptedPackageIdsAsync(Guid sessionId, CancellationToken cancellationToken)
        => (await dbContext.LoadVerificationScans
                .Where(x => x.SessionId == sessionId && x.Status == nameof(LoadVerificationScanStatus.Accepted) && x.ShipmentPackageId != null)
                .Select(x => x.ShipmentPackageId!.Value)
                .Distinct()
                .ToArrayAsync(cancellationToken))
            .ToHashSet();

    private static ShipmentPackage RehydratePackage(ShipmentPackageRecord record)
        => ShipmentPackage.Rehydrate(
            record.Id,
            record.CreatedAt,
            record.ShipmentId,
            record.ShipmentItemId,
            record.PackagingId,
            record.RouteStopId,
            ParseEnum<ShipmentPackageType>(record.PackageType, "SHIPMENT_PACKAGE_TYPE_INVALID"),
            record.PackageCount,
            record.QuantityBasePerPackage,
            record.EnteredQuantity,
            record.PackageCode,
            record.PackagingSnapshot,
            record.PhysicalSnapshot,
            record.SplitAllowed,
            ParseEnum<ShipmentPackageStatus>(record.Status, "SHIPMENT_PACKAGE_STATUS_INVALID"));

    private static LoadVerificationScanRecord ToRecord(LoadVerificationScan scan)
        => new()
        {
            Id = scan.Id,
            SessionId = scan.SessionId,
            LoadPlanId = scan.LoadPlanId,
            ShipmentId = scan.ShipmentId,
            ShipmentPackageId = scan.ShipmentPackageId,
            ExpectedLoadUnitId = scan.ExpectedLoadUnitId,
            ActualLoadUnitId = scan.ActualLoadUnitId,
            Barcode = scan.Barcode,
            Status = scan.Status.ToString(),
            ScanMode = scan.ScanMode.ToString(),
            QuantityBase = scan.QuantityBase,
            ReasonCode = scan.ReasonCode,
            ReasonText = scan.ReasonText,
            ScannedBy = scan.ScannedBy,
            ScannedAt = scan.CreatedAt,
            IdempotencyKey = scan.IdempotencyKey,
            CorrelationId = scan.CorrelationId,
            RowVersion = scan.RowVersion,
        };

    private static LoadVerificationScanDto Map(LoadVerificationScanRecord record)
        => new(
            record.Id,
            record.SessionId,
            record.LoadPlanId,
            record.ShipmentId,
            record.ShipmentPackageId,
            record.ExpectedLoadUnitId,
            record.ActualLoadUnitId,
            record.Barcode,
            record.Status,
            record.ScanMode,
            record.QuantityBase,
            record.ReasonCode,
            record.ReasonText,
            record.ScannedBy,
            record.ScannedAt,
            record.IdempotencyKey,
            record.CorrelationId,
            record.RowVersion);

    private static LoadVerificationSessionDto Map(
        LoadVerificationSessionRecord record,
        IReadOnlyCollection<LoadVerificationScanRecord> scans)
        => new(
            record.Id,
            record.LoadPlanId,
            record.ShipmentId,
            record.Status,
            record.StartedBy,
            record.StartedAt,
            record.CompletedBy,
            record.CompletedAt,
            record.CompletionReason,
            scans.Select(Map).ToArray(),
            record.CreatedAt,
            record.UpdatedAt,
            record.RowVersion);

    private static void EnsureExpectedVersion(long actual, long expected, string entityType, Guid entityId)
    {
        if (actual != expected)
        {
            throw new DomainException(new(
                "RESOURCE_VERSION_CONFLICT",
                $"{entityType} {entityId} güncel değil; beklenen row version: {expected}, mevcut: {actual}."));
        }
    }

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
        => idempotencyStore.SaveAsync(
            scope,
            key,
            payloadHash,
            statusCode,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);

    private static string ComputePayloadHash<T>(T payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));

    private static T ParseEnum<T>(string value, string code) where T : struct, Enum
        => Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result)
            ? result
            : throw new DomainException(new(code, $"Geçersiz değer: {value}."));
}
