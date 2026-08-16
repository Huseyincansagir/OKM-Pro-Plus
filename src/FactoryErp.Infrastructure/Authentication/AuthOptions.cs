namespace FactoryErp.Infrastructure.Authentication;

public sealed class AuthOptions
{
    public const string SectionName = "Authentication";

    public string Issuer { get; set; } = "factory-erp";
    public string Audience { get; set; } = "factory-erp-clients";
    public string SigningKey { get; set; } = "development-only-signing-key-change-before-production-2026";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
