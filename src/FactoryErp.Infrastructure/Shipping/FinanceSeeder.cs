using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Shipping;

public sealed class FinanceSeeder(FactoryErpDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await dbContext.TaxCodes.AnyAsync(x => x.Code == "VAT20", cancellationToken))
        {
            dbContext.TaxCodes.Add(new TaxCodeRecord
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000101"),
                Code = "VAT20",
                Name = "%20 KDV",
                Rate = 0.20m,
                ValidFrom = DateTimeOffset.UtcNow.Date,
                IsActive = true,
            });
        }

        if (!await dbContext.TaxCodes.AnyAsync(x => x.Code == "VAT0", cancellationToken))
        {
            dbContext.TaxCodes.Add(new TaxCodeRecord
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000102"),
                Code = "VAT0",
                Name = "%0 KDV",
                Rate = 0m,
                ValidFrom = DateTimeOffset.UtcNow.Date,
                IsActive = true,
            });
        }

        if (!await dbContext.PaymentMethods.AnyAsync(x => x.Code == "BANK", cancellationToken))
        {
            dbContext.PaymentMethods.Add(new PaymentMethodRecord
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000201"),
                Code = "BANK",
                Name = "Banka Havalesi",
                IsActive = true,
            });
        }

        if (!await dbContext.PaymentMethods.AnyAsync(x => x.Code == "CASH", cancellationToken))
        {
            dbContext.PaymentMethods.Add(new PaymentMethodRecord
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000202"),
                Code = "CASH",
                Name = "Nakit",
                IsActive = true,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
