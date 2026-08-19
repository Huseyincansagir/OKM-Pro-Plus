using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Hr;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Hr;

public sealed class EmployeeDirectoryService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IEmployeeDirectoryService
{
    public async Task<IReadOnlyCollection<EmployeeDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Employees
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<EmployeeDto?> GetAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == employeeId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<EmployeeDto> CreateAsync(
        CreateEmployeeRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstBlank(request.FullName, "EMPLOYEE_NAME_REQUIRED", "Personel adı zorunludur.");
        var scope = $"employee:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<EmployeeDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var now = DateTimeOffset.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = new EmployeeRecord
        {
            Id = Guid.NewGuid(),
            Code = await NextNumberAsync("employee", "PER", now, cancellationToken),
            FullName = request.FullName.Trim(),
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim(),
            Status = "Active",
            HiredOn = request.HiredOn,
            CreatedAt = now,
        };
        dbContext.Employees.Add(record);
        await auditWriter.AppendAsync(new(
            "EmployeeCreated",
            nameof(EmployeeRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.Code, record.FullName })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await idempotencyStore.SaveAsync(scope, idempotencyKey, payloadHash, 201, JsonSerializer.Serialize(result), now.AddDays(30), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static EmployeeDto Map(EmployeeRecord record)
        => new(record.Id, record.Code, record.FullName, record.Title, record.Department, record.Status, record.HiredOn, record.CreatedAt);

    private async Task<string> NextNumberAsync(string documentType, string prefix, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var year = now.Year;
        var sequence = await dbContext.DocumentSequences
            .FromSqlInterpolated($"SELECT * FROM document_sequences WHERE document_type = {documentType} AND year = {year} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (sequence is null)
        {
            sequence = new DocumentSequenceRecord { Id = Guid.NewGuid(), DocumentType = documentType, Year = year, CurrentValue = 1, UpdatedAt = now };
            dbContext.DocumentSequences.Add(sequence);
        }
        else
        {
            sequence.CurrentValue++;
            sequence.UpdatedAt = now;
        }

        return $"{prefix}-{year}-{sequence.CurrentValue:D6}";
    }

    private async Task<T?> TryReplayAsync<T>(string scope, string key, string payloadHash, CancellationToken cancellationToken)
    {
        var stored = await idempotencyStore.FindAsync(scope, key, cancellationToken);
        if (stored is null)
        {
            return default;
        }

        if (!string.Equals(stored.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(new("IDEMPOTENCY_PAYLOAD_MISMATCH", "Aynı Idempotency-Key farklı payload ile tekrar kullanılamaz."));
        }

        return JsonSerializer.Deserialize<T>(stored.ResponseBody);
    }

    private static string ComputePayloadHash(object payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))).ToLowerInvariant();
}
