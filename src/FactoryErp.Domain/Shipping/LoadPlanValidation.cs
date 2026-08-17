using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Shipping;

public enum LoadPlanValidationSeverity
{
    HardError,
    Warning,
    Info,
}

public enum LoadPlanValidationResolutionStatus
{
    Open,
    Resolved,
    Overridden,
    NotApplicable,
}

public enum LoadPlanManualChangeType
{
    AddLoadUnit,
    RemoveLoadUnit,
    MovePackage,
    ChangeQuantity,
    ChangeStopAllocation,
    ChangeVehicle,
    ChangeCapacity,
    Other,
}

public sealed class LoadPlanValidationResult : Entity
{
    private LoadPlanValidationResult(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadPlanId,
        string validationKey,
        LoadPlanValidationSeverity severity,
        string code,
        string message,
        string? entityType,
        Guid? entityId)
        : base(id, createdAt)
    {
        LoadPlanId = loadPlanId;
        ValidationKey = validationKey;
        Severity = severity;
        Code = code;
        Message = message;
        EntityType = entityType;
        EntityId = entityId;
        ResolutionStatus = LoadPlanValidationResolutionStatus.Open;
    }

    public Guid LoadPlanId { get; }
    public string ValidationKey { get; }
    public LoadPlanValidationSeverity Severity { get; }
    public string Code { get; }
    public string Message { get; }
    public string? EntityType { get; }
    public Guid? EntityId { get; }
    public LoadPlanValidationResolutionStatus ResolutionStatus { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolutionReason { get; private set; }

    public static LoadPlanValidationResult Create(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadPlanId,
        string validationKey,
        LoadPlanValidationSeverity severity,
        string code,
        string message,
        string? entityType = null,
        Guid? entityId = null)
    {
        DomainGuard.AgainstEmpty(id, "VALIDATION_RESULT_ID_REQUIRED", "Validation result kimliği zorunludur.");
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "Validation result LoadPlan kaydına bağlı olmalıdır.");
        DomainGuard.AgainstBlank(validationKey, "VALIDATION_KEY_REQUIRED", "Validation key zorunludur.");
        DomainGuard.AgainstBlank(code, "VALIDATION_CODE_REQUIRED", "Validation code zorunludur.");
        DomainGuard.AgainstBlank(message, "VALIDATION_MESSAGE_REQUIRED", "Validation message zorunludur.");
        if (!Enum.IsDefined(severity))
        {
            throw new DomainException(new("VALIDATION_SEVERITY_INVALID", "Validation severity geçersizdir."));
        }

        if (entityId == Guid.Empty)
        {
            throw new DomainException(new("VALIDATION_ENTITY_INVALID", "Validation entity kimliği geçerli olmalıdır."));
        }

        return new LoadPlanValidationResult(
            id,
            createdAt,
            loadPlanId,
            validationKey.Trim(),
            severity,
            code.Trim(),
            message.Trim(),
            string.IsNullOrWhiteSpace(entityType) ? null : entityType.Trim(),
            entityId);
    }

    public void Resolve(
        LoadPlanValidationResolutionStatus resolutionStatus,
        Guid actorId,
        string reason,
        DateTimeOffset resolvedAt)
    {
        if (resolutionStatus is LoadPlanValidationResolutionStatus.Open)
        {
            throw new DomainException(new("VALIDATION_RESOLUTION_INVALID", "Validation result Open durumuna resolve edilemez."));
        }

        DomainGuard.AgainstEmpty(actorId, "VALIDATION_RESOLVER_REQUIRED", "Validation resolver zorunludur.");
        DomainGuard.AgainstBlank(reason, "VALIDATION_RESOLUTION_REASON_REQUIRED", "Validation resolution reason zorunludur.");
        if (ResolutionStatus is not LoadPlanValidationResolutionStatus.Open)
        {
            throw new DomainException(new("VALIDATION_ALREADY_RESOLVED", "Validation result yalnızca Open durumdayken resolve edilebilir."));
        }

        ResolutionStatus = resolutionStatus;
        ResolvedBy = actorId;
        ResolvedAt = resolvedAt;
        ResolutionReason = reason.Trim();
    }
}

public sealed class LoadPlanManualChange : Entity
{
    private LoadPlanManualChange(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadPlanId,
        Guid actorUserId,
        LoadPlanManualChangeType changeType,
        string entityType,
        Guid entityId,
        string beforeJson,
        string afterJson,
        string reason)
        : base(id, createdAt)
    {
        LoadPlanId = loadPlanId;
        ActorUserId = actorUserId;
        ChangeType = changeType;
        EntityType = entityType;
        EntityId = entityId;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        Reason = reason;
    }

    public Guid LoadPlanId { get; }
    public Guid ActorUserId { get; }
    public LoadPlanManualChangeType ChangeType { get; }
    public string EntityType { get; }
    public Guid EntityId { get; }
    public string BeforeJson { get; }
    public string AfterJson { get; }
    public string Reason { get; }

    public static LoadPlanManualChange Create(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadPlanId,
        Guid actorUserId,
        LoadPlanManualChangeType changeType,
        string entityType,
        Guid entityId,
        string beforeJson,
        string afterJson,
        string reason)
    {
        DomainGuard.AgainstEmpty(id, "MANUAL_CHANGE_ID_REQUIRED", "Manual change kimliği zorunludur.");
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "Manual change LoadPlan kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(actorUserId, "MANUAL_CHANGE_ACTOR_REQUIRED", "Manual change actor zorunludur.");
        DomainGuard.AgainstBlank(entityType, "MANUAL_CHANGE_ENTITY_TYPE_REQUIRED", "Manual change entity type zorunludur.");
        DomainGuard.AgainstEmpty(entityId, "MANUAL_CHANGE_ENTITY_REQUIRED", "Manual change entity kimliği zorunludur.");
        DomainGuard.AgainstBlank(beforeJson, "MANUAL_CHANGE_BEFORE_REQUIRED", "Manual change before snapshot zorunludur.");
        DomainGuard.AgainstBlank(afterJson, "MANUAL_CHANGE_AFTER_REQUIRED", "Manual change after snapshot zorunludur.");
        DomainGuard.AgainstBlank(reason, "MANUAL_CHANGE_REASON_REQUIRED", "Manual change reason zorunludur.");
        if (!Enum.IsDefined(changeType))
        {
            throw new DomainException(new("MANUAL_CHANGE_TYPE_INVALID", "Manual change tipi geçersizdir."));
        }

        return new LoadPlanManualChange(
            id,
            createdAt,
            loadPlanId,
            actorUserId,
            changeType,
            entityType.Trim(),
            entityId,
            beforeJson.Trim(),
            afterJson.Trim(),
            reason.Trim());
    }
}

public static class LoadPlanLockPolicy
{
    public static void EnsureLockAllowed(
        LoadPlanStatus status,
        LoadPlanFeasibilityStatus feasibilityStatus,
        bool hasOpenHardErrors,
        bool hasOpenWarnings,
        bool approval,
        bool warningOverrideApproved,
        Guid? vehicleId,
        Guid? vehicleCapacityId,
        string? inputSnapshotHash)
    {
        if (status is not (LoadPlanStatus.Valid or LoadPlanStatus.NeedsReview))
        {
            throw new DomainException(new("LOAD_PLAN_INVALID_TRANSITION", $"{status} durumundaki plan Locked durumuna geçemez."));
        }

        if (!approval)
        {
            throw new DomainException(new("LOAD_PLAN_APPROVAL_REQUIRED", "LoadPlan lock için depo sorumlusu onayı zorunludur."));
        }

        if (hasOpenHardErrors || feasibilityStatus == LoadPlanFeasibilityStatus.Infeasible)
        {
            throw new DomainException(new("LOAD_PLAN_INFEASIBLE", "Hard validation hatası olan plan lock edilemez."));
        }

        if (hasOpenWarnings && !warningOverrideApproved)
        {
            throw new DomainException(new("LOAD_PLAN_APPROVAL_REQUIRED", "Çözülmemiş warning için override veya resolution zorunludur."));
        }

        DomainGuard.AgainstEmpty(vehicleId ?? Guid.Empty, "LOCKED_VEHICLE_REQUIRED", "Locked LoadPlan vehicle kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(vehicleCapacityId ?? Guid.Empty, "LOCKED_CAPACITY_REQUIRED", "Locked LoadPlan vehicle capacity kaydına bağlı olmalıdır.");
        DomainGuard.AgainstBlank(inputSnapshotHash, "LOCKED_SNAPSHOT_REQUIRED", "Locked LoadPlan input snapshot hash içermelidir.");
    }
}
