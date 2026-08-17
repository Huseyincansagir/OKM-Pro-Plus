using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Shipping;

public enum LoadVerificationSessionStatus
{
    Draft,
    InProgress,
    Completed,
    Discrepancy,
    Cancelled,
}

public enum LoadVerificationScanStatus
{
    Accepted,
    Duplicate,
    Unexpected,
    WrongUnit,
    CancelledPackage,
    Discrepancy,
}

public enum LoadVerificationScanMode
{
    Pallet,
    Case,
    Package,
    BaseUnit,
}

public sealed class LoadVerificationSession : AggregateRoot
{
    private readonly HashSet<Guid> _acceptedPackageIds = [];

    private LoadVerificationSession(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadPlanId,
        Guid shipmentId,
        Guid startedBy)
        : base(id, createdAt)
    {
        LoadPlanId = loadPlanId;
        ShipmentId = shipmentId;
        StartedBy = startedBy;
        StartedAt = createdAt;
        Status = LoadVerificationSessionStatus.Draft;
    }

    public Guid LoadPlanId { get; }
    public Guid ShipmentId { get; }
    public LoadVerificationSessionStatus Status { get; private set; }
    public Guid StartedBy { get; }
    public DateTimeOffset StartedAt { get; }
    public Guid? CompletedBy { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? CompletionReason { get; private set; }
    public IReadOnlyCollection<Guid> AcceptedPackageIds => _acceptedPackageIds;

    public static LoadVerificationSession Create(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadPlanId,
        Guid shipmentId,
        Guid startedBy)
    {
        DomainGuard.AgainstEmpty(id, "LOAD_VERIFICATION_SESSION_ID_REQUIRED", "Load verification session kimliği zorunludur.");
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "Load verification session LoadPlan kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "Load verification session Shipment kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(startedBy, "LOAD_VERIFICATION_STARTER_REQUIRED", "Load verification session başlatan kullanıcı zorunludur.");

        return new LoadVerificationSession(id, createdAt, loadPlanId, shipmentId, startedBy);
    }

    public void Start(DateTimeOffset now)
    {
        if (Status != LoadVerificationSessionStatus.Draft)
        {
            throw new DomainException(new(
                "LOAD_VERIFICATION_INVALID_TRANSITION",
                $"{Status} durumundaki session başlatılamaz."));
        }

        Status = LoadVerificationSessionStatus.InProgress;
        Touch(now);
    }

    public void AcceptPackage(Guid shipmentPackageId, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(shipmentPackageId, "SHIPMENT_PACKAGE_REQUIRED", "Yüklenen ShipmentPackage zorunludur.");
        EnsureInProgress();
        if (!_acceptedPackageIds.Add(shipmentPackageId))
        {
            throw new DomainException(new(
                "PACKAGE_ALREADY_LOADED",
                "ShipmentPackage aynı load verification session içinde daha önce kabul edilmiştir."));
        }

        Touch(now);
    }

    public void Complete(Guid actorId, bool allExpectedPackagesAccepted, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(actorId, "LOAD_VERIFICATION_COMPLETER_REQUIRED", "Load verification tamamlayan kullanıcı zorunludur.");
        EnsureInProgress();
        if (!allExpectedPackagesAccepted)
        {
            throw new DomainException(new(
                "LOAD_VERIFICATION_INCOMPLETE",
                "Tüm beklenen ShipmentPackage kayıtları kabul edilmeden session tamamlanamaz."));
        }

        Status = LoadVerificationSessionStatus.Completed;
        CompletedBy = actorId;
        CompletedAt = now;
        CompletionReason = null;
        Touch(now);
    }

    public void CloseAsDiscrepancy(Guid actorId, string reason, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(actorId, "LOAD_VERIFICATION_CLOSER_REQUIRED", "Discrepancy kapatan kullanıcı zorunludur.");
        DomainGuard.AgainstBlank(reason, "LOAD_VERIFICATION_DISCREPANCY_REASON_REQUIRED", "Discrepancy kapanış gerekçesi zorunludur.");
        EnsureInProgress();

        Status = LoadVerificationSessionStatus.Discrepancy;
        CompletedBy = actorId;
        CompletedAt = now;
        CompletionReason = reason.Trim();
        Touch(now);
    }

    public void Cancel(Guid actorId, string reason, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(actorId, "LOAD_VERIFICATION_CANCELLER_REQUIRED", "Session iptal eden kullanıcı zorunludur.");
        DomainGuard.AgainstBlank(reason, "LOAD_VERIFICATION_CANCEL_REASON_REQUIRED", "Session iptal gerekçesi zorunludur.");
        if (Status is LoadVerificationSessionStatus.Completed or LoadVerificationSessionStatus.Cancelled)
        {
            throw new DomainException(new(
                "LOAD_VERIFICATION_INVALID_TRANSITION",
                $"{Status} durumundaki session iptal edilemez."));
        }

        Status = LoadVerificationSessionStatus.Cancelled;
        CompletedBy = actorId;
        CompletedAt = now;
        CompletionReason = reason.Trim();
        Touch(now);
    }

    public static void EnsureStartAllowed(
        LoadPlanStatus loadPlanStatus,
        LoadVerificationSessionStatus? activeSessionStatus)
    {
        if (loadPlanStatus != LoadPlanStatus.Locked)
        {
            throw new DomainException(new(
                "LOAD_PLAN_NOT_LOCKED",
                "Yükleme doğrulaması yalnızca Locked LoadPlan üzerinde başlatılabilir."));
        }

        if (activeSessionStatus is LoadVerificationSessionStatus.Draft or LoadVerificationSessionStatus.InProgress)
        {
            throw new DomainException(new(
                "LOAD_VERIFICATION_ACTIVE_SESSION",
                "LoadPlan için zaten aktif bir load verification session bulunmaktadır."));
        }
    }

    private void EnsureInProgress()
    {
        if (Status != LoadVerificationSessionStatus.InProgress)
        {
            throw new DomainException(new(
                "LOAD_VERIFICATION_INVALID_STATE",
                $"{Status} durumundaki session bu komut için uygun değildir."));
        }
    }
}

public sealed class LoadVerificationScan : Entity
{
    private LoadVerificationScan(
        Guid id,
        DateTimeOffset scannedAt,
        Guid sessionId,
        Guid loadPlanId,
        Guid shipmentId,
        Guid? shipmentPackageId,
        Guid? expectedLoadUnitId,
        Guid? actualLoadUnitId,
        string barcode,
        LoadVerificationScanStatus status,
        LoadVerificationScanMode scanMode,
        decimal quantityBase,
        string? reasonCode,
        string? reasonText,
        Guid scannedBy,
        string idempotencyKey,
        string correlationId)
        : base(id, scannedAt)
    {
        SessionId = sessionId;
        LoadPlanId = loadPlanId;
        ShipmentId = shipmentId;
        ShipmentPackageId = shipmentPackageId;
        ExpectedLoadUnitId = expectedLoadUnitId;
        ActualLoadUnitId = actualLoadUnitId;
        Barcode = barcode;
        Status = status;
        ScanMode = scanMode;
        QuantityBase = quantityBase;
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? null : reasonCode.Trim();
        ReasonText = string.IsNullOrWhiteSpace(reasonText) ? null : reasonText.Trim();
        ScannedBy = scannedBy;
        IdempotencyKey = idempotencyKey;
        CorrelationId = correlationId;
    }

    public Guid SessionId { get; }
    public Guid LoadPlanId { get; }
    public Guid ShipmentId { get; }
    public Guid? ShipmentPackageId { get; }
    public Guid? ExpectedLoadUnitId { get; }
    public Guid? ActualLoadUnitId { get; }
    public string Barcode { get; }
    public LoadVerificationScanStatus Status { get; }
    public LoadVerificationScanMode ScanMode { get; }
    public decimal QuantityBase { get; }
    public string? ReasonCode { get; }
    public string? ReasonText { get; }
    public Guid ScannedBy { get; }
    public string IdempotencyKey { get; }
    public string CorrelationId { get; }

    public static LoadVerificationScan Create(
        Guid id,
        DateTimeOffset scannedAt,
        Guid sessionId,
        Guid loadPlanId,
        Guid shipmentId,
        Guid? shipmentPackageId,
        Guid? expectedLoadUnitId,
        Guid? actualLoadUnitId,
        string barcode,
        LoadVerificationScanStatus status,
        LoadVerificationScanMode scanMode,
        decimal quantityBase,
        string? reasonCode,
        string? reasonText,
        Guid scannedBy,
        string idempotencyKey,
        string correlationId)
    {
        DomainGuard.AgainstEmpty(id, "LOAD_VERIFICATION_SCAN_ID_REQUIRED", "Load verification scan kimliği zorunludur.");
        DomainGuard.AgainstEmpty(sessionId, "LOAD_VERIFICATION_SESSION_REQUIRED", "Load verification session zorunludur.");
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "Load verification scan LoadPlan kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "Load verification scan Shipment kaydına bağlı olmalıdır.");
        DomainGuard.AgainstBlank(barcode, "PACKAGE_BARCODE_REQUIRED", "ShipmentPackage barkodu zorunludur.");
        DomainGuard.AgainstEmpty(scannedBy, "LOAD_VERIFICATION_SCANNER_REQUIRED", "Barkodu okuyan kullanıcı zorunludur.");
        DomainGuard.AgainstBlank(idempotencyKey, "IDEMPOTENCY_KEY_REQUIRED", "Load verification scan Idempotency-Key zorunludur.");
        DomainGuard.AgainstBlank(correlationId, "CORRELATION_ID_REQUIRED", "Load verification scan correlation id zorunludur.");

        if (!Enum.IsDefined(status))
        {
            throw new DomainException(new("LOAD_VERIFICATION_SCAN_STATUS_INVALID", "Load verification scan status geçersizdir."));
        }

        if (!Enum.IsDefined(scanMode))
        {
            throw new DomainException(new("LOAD_VERIFICATION_SCAN_MODE_INVALID", "Load verification scan mode geçersizdir."));
        }

        if (quantityBase <= 0)
        {
            throw new DomainException(new("LOAD_VERIFICATION_QUANTITY_INVALID", "Load verification scan quantity_base sıfırdan büyük olmalıdır."));
        }

        if (status == LoadVerificationScanStatus.Accepted && (shipmentPackageId is null || shipmentPackageId == Guid.Empty))
        {
            throw new DomainException(new("PACKAGE_REQUIRED_FOR_ACCEPTED_SCAN", "Accepted scan ShipmentPackage kaydına bağlı olmalıdır."));
        }

        if (status == LoadVerificationScanStatus.WrongUnit && (expectedLoadUnitId is null || expectedLoadUnitId == Guid.Empty))
        {
            throw new DomainException(new("EXPECTED_LOAD_UNIT_REQUIRED", "WrongUnit sonucu için beklenen LoadUnit zorunludur."));
        }

        return new LoadVerificationScan(
            id,
            scannedAt,
            sessionId,
            loadPlanId,
            shipmentId,
            shipmentPackageId,
            expectedLoadUnitId,
            actualLoadUnitId,
            barcode.Trim(),
            status,
            scanMode,
            quantityBase,
            reasonCode,
            reasonText,
            scannedBy,
            idempotencyKey.Trim(),
            correlationId.Trim());
    }
}

public static class LoadVerificationPolicy
{
    public static void EnsurePackageCanBeAccepted(
        LoadPlanStatus loadPlanStatus,
        LoadVerificationSessionStatus sessionStatus,
        ShipmentPackageStatus packageStatus,
        bool packageBelongsToLoadPlan,
        Guid? expectedLoadUnitId,
        Guid? actualLoadUnitId)
    {
        if (loadPlanStatus != LoadPlanStatus.Locked)
        {
            throw new DomainException(new("LOAD_PLAN_NOT_LOCKED", "Yükleme doğrulaması yalnızca Locked LoadPlan üzerinde yapılabilir."));
        }

        if (sessionStatus != LoadVerificationSessionStatus.InProgress)
        {
            throw new DomainException(new("LOAD_VERIFICATION_INVALID_STATE", "Load verification session scan için InProgress olmalıdır."));
        }

        if (!packageBelongsToLoadPlan)
        {
            throw new DomainException(new("PACKAGE_NOT_IN_LOAD_PLAN", "ShipmentPackage bu locked LoadPlan içinde bulunmamaktadır."));
        }

        if (packageStatus == ShipmentPackageStatus.Cancelled)
        {
            throw new DomainException(new("PACKAGE_CANCELLED", "Cancelled ShipmentPackage yüklenemez."));
        }

        if (packageStatus == ShipmentPackageStatus.Loaded)
        {
            throw new DomainException(new("PACKAGE_ALREADY_LOADED", "ShipmentPackage daha önce Loaded durumuna geçmiştir."));
        }

        if (expectedLoadUnitId is not null && actualLoadUnitId is not null && expectedLoadUnitId != actualLoadUnitId)
        {
            throw new DomainException(new("LOAD_UNIT_MISMATCH", "Okutulan paket beklenen LoadUnit ile eşleşmemektedir."));
        }
    }

    public static void EnsureCompletionAllowed(
        LoadVerificationSessionStatus sessionStatus,
        bool allExpectedPackagesAccepted)
    {
        if (sessionStatus != LoadVerificationSessionStatus.InProgress)
        {
            throw new DomainException(new("LOAD_VERIFICATION_INVALID_STATE", "Yalnızca InProgress session tamamlanabilir."));
        }

        if (!allExpectedPackagesAccepted)
        {
            throw new DomainException(new("LOAD_VERIFICATION_INCOMPLETE", "Tüm beklenen paketler kabul edilmeden load verification tamamlanamaz."));
        }
    }
}
