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
    private static readonly Guid CreditPriceListId = Guid.Parse("40000000-0000-0000-0000-000000000203");
    private static readonly Guid CreditPriceGroupId = Guid.Parse("40000000-0000-0000-0000-000000000204");
    private static readonly Guid IntegrationSalesOrderId = Guid.Parse("eade528b-1cc3-42c9-8009-93066dac6750");
    private static readonly Guid IntegrationSalesOrderItemId = Guid.Parse("eade528b-1cc3-42c9-8009-93066dac6751");
    private static readonly Guid IntegrationDeliveryNoteId = Guid.Parse("eade528b-1cc3-42c9-8009-93066dac675f");
    private static readonly Guid IntegrationDeliveryNoteItemId = Guid.Parse("eade528b-1cc3-42c9-8009-93066dac6760");

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

        var creditList = await dbContext.PriceLists.SingleOrDefaultAsync(x => x.Code == "VADELI", cancellationToken);
        if (creditList is null)
        {
            creditList = new PriceListRecord
            {
                Id = CreditPriceListId,
                Code = "VADELI",
                Name = "Vadeli müşteri TRY listesi",
                CurrencyCode = "TRY",
                ValidFrom = DateTimeOffset.UtcNow.Date,
                IsActive = true,
            };
            dbContext.PriceLists.Add(creditList);
        }

        var creditGroup = await dbContext.CustomerPriceGroups.SingleOrDefaultAsync(x => x.Code == "VADELI", cancellationToken);
        if (creditGroup is null)
        {
            creditGroup = new CustomerPriceGroupRecord
            {
                Id = CreditPriceGroupId,
                Code = "VADELI",
                Name = "Vadeli müşteri grubu",
                PriceListId = creditList.Id,
                IsActive = true,
            };
            dbContext.CustomerPriceGroups.Add(creditGroup);
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

        if (!await dbContext.ProductPrices.AnyAsync(x => x.PriceListId == creditList.Id && x.ProductId == Guid.Parse("30000000-0000-0000-0000-000000000201"), cancellationToken))
        {
            dbContext.ProductPrices.AddRange(
                new ProductPriceRecord
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000221"),
                    PriceListId = creditList.Id,
                    ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201"),
                    PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000212"),
                    UnitPrice = 14.50m,
                    CurrencyCode = "TRY",
                    ValidFrom = DateTimeOffset.UtcNow.Date,
                },
                new ProductPriceRecord
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000222"),
                    PriceListId = creditList.Id,
                    ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201"),
                    PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000213"),
                    UnitPrice = 255.00m,
                    CurrencyCode = "TRY",
                    ValidFrom = DateTimeOffset.UtcNow.Date,
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureIntegrationDeliveryNoteAsync(cancellationToken);
    }

    private async Task EnsureIntegrationDeliveryNoteAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.DeliveryNotes.AnyAsync(x => x.Id == IntegrationDeliveryNoteId, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var packagingId = Guid.Parse("30000000-0000-0000-0000-000000000213");
        var productId = Guid.Parse("30000000-0000-0000-0000-000000000201");
        var order = new SalesOrderRecord
        {
            Id = IntegrationSalesOrderId,
            OrderNumber = "SO-INTEGRATION-001",
            CustomerId = DemoCustomerId,
            Status = "Fulfilled",
            CurrencyCode = "TRY",
            TotalNet = 0m,
            TotalTax = 0m,
            TotalGross = 0m,
            RowVersion = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        order.Items.Add(new SalesOrderItemRecord
        {
            Id = IntegrationSalesOrderItemId,
            SalesOrderId = order.Id,
            ProductId = productId,
            OrderedQty = 4000m,
            ReservedQty = 0m,
            ShippedQty = 4000m,
            CancelledQty = 0m,
            RemainingQty = 0m,
            EnteredQuantity = 40m,
            EnteredPackagingId = packagingId,
            PackagingSnapshot = "{\"seed\":\"integration\"}",
            PartialDeliveryAllowed = true,
            UnitPrice = 0m,
            PriceSnapshot = "{}",
            RowVersion = 1,
        });
        var note = new DeliveryNoteRecord
        {
            Id = IntegrationDeliveryNoteId,
            DocumentNumber = "DN-INTEGRATION-001",
            SalesOrderId = order.Id,
            CustomerId = DemoCustomerId,
            Status = "Issued",
            IssuedAt = now,
            CreatedAt = now,
            RowVersion = 1,
        };
        note.Items.Add(new DeliveryNoteItemRecord
        {
            Id = IntegrationDeliveryNoteItemId,
            DeliveryNoteId = note.Id,
            SalesOrderItemId = IntegrationSalesOrderItemId,
            ProductId = productId,
            QuantityBase = 4000m,
            EnteredQuantity = 40m,
            EnteredPackagingId = packagingId,
            PackagingSnapshot = "{\"seed\":\"integration\"}",
            ShippedQty = 4000m,
            InvoicedQty = 0m,
            WaivedQty = 0m,
            RemainingToInvoice = 4000m,
            RowVersion = 1,
        });
        dbContext.SalesOrders.Add(order);
        dbContext.DeliveryNotes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
