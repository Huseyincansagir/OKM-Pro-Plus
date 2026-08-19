namespace FactoryErp.Application.Hr;

public sealed record EmployeeDto(
    Guid Id,
    string Code,
    string FullName,
    string? Title,
    string? Department,
    string Status,
    DateOnly? HiredOn,
    DateTimeOffset CreatedAt);

public sealed record CreateEmployeeRequest(
    string FullName,
    string? Title,
    string? Department,
    DateOnly? HiredOn);

public interface IEmployeeDirectoryService
{
    Task<IReadOnlyCollection<EmployeeDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeDto> CreateAsync(
        CreateEmployeeRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);
}
