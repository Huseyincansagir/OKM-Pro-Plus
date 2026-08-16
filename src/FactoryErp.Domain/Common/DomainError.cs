namespace FactoryErp.Domain.Common;

public sealed record DomainError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, object?>? Metadata = null);
