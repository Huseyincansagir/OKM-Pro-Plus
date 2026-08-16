using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Sales;

public sealed class SalesSeeder(FactoryErpDbContext dbContext)
{
    private static readonly Guid DemoCustomerId = Guid.Parse("40000000-0000-0000-0000-000000000101");
    private static readonly Guid DemoAddressId = Guid.Parse("40000000-0000-0000-0000-000000000102");
    private static readonly Guid DemoContactId = Guid.Parse("40000000-0000-0000-0000-000000000103");
    private static readonly Guid DefaultPriceListId = Guid.Parse("40000000-0000-0000-0000-000000000201");
    private static readonly Guid StandardPriceGroupId = Guid.Parse("40000000-0000-0000-0000-000000000202");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .Include(x => x.Addresses)
            .Include(x => x.Contacts)
            .SingleOrDefaultAsync(x => x.CustomerCode == "DEMO-001", cancellationToken);
        if (customer is null)
        {
            customer = new CustomerRecord
            {
                Id = DemoCustomerId,
                CustomerCode = "DEMO-001",
                LegalName = "Demo Horeca Tedarik",
                Email = "demo@example.local",
                Phone = "+90 555 000 00 01",
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            customer.Addresses.Add(new CustomerAddressRecord
            {
                Id = DemoAddressId,
                CustomerId = customer.Id,
                AddressType = "Delivery",
                Title = "Ana teslimat adresi",
                Line1 = "Organize Sanayi Bölgesi 1. Cadde No:1",
                District = "Merkez",
                City = "İstanbul",
                CountryCode = "TR",
                IsDefault = true,
                IsActive = true,
            });
            customer.Contacts.Add(new CustomerContactRecord
            {
                Id = DemoContactId,
                CustomerId = customer.Id,
                FullName = "Demo Yetkili",
                Email = customer.Email,
                Phone = customer.Phone,
                RoleTitle = "Satın Alma",
                IsPrimary = true,
                IsActive = true,
            });
            dbContext.Customers.Add(customer);
        }

        var priceList = await dbContext.PriceLists.SingleOrDefaultAsync(x => x.Code == "DEFAULT", cancellationToken);
        if (priceList is null)
        {
            priceList = new PriceListRecord
            {
                Id = DefaultPriceListId,
                Code = "DEFAULT",
                Name = "Varsayılan TRY Fiyat Listesi",
                CurrencyCode = "TRY",
                ValidFrom = DateTimeOffset.UtcNow.Date,
                IsActive = true,
            };
            dbContext.PriceLists.Add(priceList);
        }

        var group = await dbContext.CustomerPriceGroups.SingleOrDefaultAsync(x => x.Code == "STANDARD", cancellationToken);
        if (group is null)
        {
            group = new CustomerPriceGroupRecord
            {
                Id = StandardPriceGroupId,
                Code = "STANDARD",
                Name = "Standart Müşteri Grubu",
                PriceListId = priceList.Id,
                IsActive = true,
            };
            dbContext.CustomerPriceGroups.Add(group);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!await dbContext.CustomerPriceGroupMembers.AnyAsync(x => x.CustomerId == customer.Id && x.CustomerPriceGroupId == group.Id, cancellationToken))
        {
            dbContext.CustomerPriceGroupMembers.Add(new CustomerPriceGroupMemberRecord
            {
                CustomerId = customer.Id,
                CustomerPriceGroupId = group.Id,
                EffectiveFrom = DateTimeOffset.UtcNow.Date,
            });
        }

        if (!await dbContext.ProductPrices.AnyAsync(x => x.PriceListId == priceList.Id && x.ProductId == Guid.Parse("30000000-0000-0000-0000-000000000201"), cancellationToken))
        {
            dbContext.ProductPrices.AddRange(
                new ProductPriceRecord
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000211"),
                    PriceListId = priceList.Id,
                    ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201"),
                    PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000212"),
                    UnitPrice = 12.50m,
                    CurrencyCode = "TRY",
                    ValidFrom = DateTimeOffset.UtcNow.Date,
                },
                new ProductPriceRecord
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000212"),
                    PriceListId = priceList.Id,
                    ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201"),
                    PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000213"),
                    UnitPrice = 220.00m,
                    CurrencyCode = "TRY",
                    ValidFrom = DateTimeOffset.UtcNow.Date,
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
