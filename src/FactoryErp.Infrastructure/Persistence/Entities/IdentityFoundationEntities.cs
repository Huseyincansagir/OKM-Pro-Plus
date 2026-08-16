namespace FactoryErp.Infrastructure.Persistence.Entities;

public sealed class UserRecord
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public ICollection<UserRoleRecord> UserRoles { get; } = new List<UserRoleRecord>();
}

public sealed class RoleRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }

    public ICollection<UserRoleRecord> UserRoles { get; } = new List<UserRoleRecord>();
    public ICollection<RolePermissionRecord> RolePermissions { get; } = new List<RolePermissionRecord>();
}

public sealed class PermissionRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<RolePermissionRecord> RolePermissions { get; } = new List<RolePermissionRecord>();
}

public sealed class UserRoleRecord
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public Guid? AssignedBy { get; set; }

    public UserRecord User { get; set; } = null!;
    public RoleRecord Role { get; set; } = null!;
}

public sealed class RolePermissionRecord
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }

    public RoleRecord Role { get; set; } = null!;
    public PermissionRecord Permission { get; set; } = null!;
}

public sealed class RefreshTokenRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public UserRecord User { get; set; } = null!;
}

public sealed class AuditLogRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class SystemSettingRecord
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public sealed class DocumentSequenceRecord
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public int Year { get; set; }
    public long CurrentValue { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class OutboxMessageRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
