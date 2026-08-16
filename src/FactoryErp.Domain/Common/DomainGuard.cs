namespace FactoryErp.Domain.Common;

public static class DomainGuard
{
    public static void AgainstEmpty(Guid value, string code, string message)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(new(code, message));
        }
    }

    public static void AgainstBlank(string? value, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(new(code, message));
        }
    }
}
