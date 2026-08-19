using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Sales;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Sales;

public sealed class PricingCommandService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : ISalesPricingService
{
    public async Task<IReadOnlyCollection<PriceListDto>> ListPriceListsAsync(CancellationToken cancellationToken = default)
    {
        var lists = await dbContext.PriceLists
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return lists.Select(MapPriceList).ToArray();
    }

    public async Task<PriceListDetailDto?> GetPriceListAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.PriceLists
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == priceListId, cancellationToken);
        if (list is null)
        {
            return null;
        }

        var prices = await dbContext.ProductPrices
            .AsNoTracking()
            .Where(x => x.PriceListId == priceListId)
            .OrderBy(x => x.ProductId)
            .ThenBy(x => x.ValidFrom)
            .Take(200)
            .ToArrayAsync(cancellationToken);
        return new PriceListDetailDto(
            list.Id,
            list.Code,
            list.Name,
            list.CurrencyCode,
            list.ValidFrom,
            list.ValidTo,
            list.IsActive,
            prices.Select(MapProductPrice).ToArray());
    }

    public async Task<PriceListDto> CreatePriceListAsync(
        CreatePriceListRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DomainException(new("PRICE_LIST_INVALID", "Fiyat listesi kodu ve adı zorunludur."));
        }

        var currency = string.IsNullOrWhiteSpace(request.CurrencyCode)
            ? "TRY"
            : request.CurrencyCode.Trim().ToUpperInvariant();
        if (currency != "TRY")
        {
            throw new DomainException(new("PRICE_CURRENCY_UNSUPPORTED", "MVP yalnızca TRY fiyat listesini kabul eder."));
        }

        var now = DateTimeOffset.UtcNow;
        var validFrom = request.ValidFrom ?? now;
        if (request.ValidTo is { } validTo && validTo <= validFrom)
        {
            throw new DomainException(new("PRICE_LIST_VALID_WINDOW", "Geçerlilik bitişi başlangıçtan sonra olmalıdır."));
        }

        var idempotencyScope = $"price-list:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<PriceListDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.PriceLists.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw new DomainException(new("PRICE_LIST_CODE_TAKEN", "Bu fiyat listesi kodu zaten var."));
        }

        var list = new PriceListRecord
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            CurrencyCode = currency,
            ValidFrom = validFrom,
            ValidTo = request.ValidTo,
            IsActive = true,
        };
        dbContext.PriceLists.Add(list);
        await auditWriter.AppendAsync(new(
            "PriceListCreated",
            nameof(PriceListRecord),
            list.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { list.Code, list.Name, list.CurrencyCode })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapPriceList(list);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            201,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<ProductPriceDto> AddProductPriceAsync(
        Guid productId,
        CreateProductPriceRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (request.UnitPrice < 0)
        {
            throw new DomainException(new("PRICE_INVALID", "Birim fiyat negatif olamaz."));
        }

        var now = DateTimeOffset.UtcNow;
        var validFrom = request.ValidFrom ?? now;
        if (request.ValidTo is { } validTo && validTo <= validFrom)
        {
            throw new DomainException(new("PRICE_LIST_VALID_WINDOW", "Geçerlilik bitişi başlangıçtan sonra olmalıdır."));
        }

        var idempotencyScope = $"product-price:create:{actorId}:{productId}";
        var payloadHash = ComputePayloadHash(new { productId, request });
        var replay = await TryReplayAsync<ProductPriceDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var list = await dbContext.PriceLists.SingleOrDefaultAsync(x => x.Id == request.PriceListId, cancellationToken);
        if (list is null || !list.IsActive)
        {
            throw new DomainException(new("PRICE_LIST_NOT_FOUND", "Aktif fiyat listesi bulunamadı."));
        }

        var productExists = await dbContext.Products.AnyAsync(x => x.Id == productId && x.IsActive, cancellationToken);
        if (!productExists)
        {
            throw new DomainException(new("PRODUCT_OR_PACKAGING_NOT_FOUND", "Ürün bulunamadı."));
        }

        if (request.PackagingId.HasValue)
        {
            var packagingExists = await dbContext.ProductPackagings.AnyAsync(
                x => x.Id == request.PackagingId.Value && x.ProductId == productId,
                cancellationToken);
            if (!packagingExists)
            {
                throw new DomainException(new("PRODUCT_OR_PACKAGING_NOT_FOUND", "Ürün ambalajı bulunamadı."));
            }
        }

        var price = new ProductPriceRecord
        {
            Id = Guid.NewGuid(),
            PriceListId = list.Id,
            ProductId = productId,
            PackagingId = request.PackagingId,
            UnitPrice = request.UnitPrice,
            CurrencyCode = list.CurrencyCode,
            TaxCode = string.IsNullOrWhiteSpace(request.TaxCode) ? null : request.TaxCode.Trim(),
            ValidFrom = validFrom,
            ValidTo = request.ValidTo,
        };
        dbContext.ProductPrices.Add(price);
        await auditWriter.AppendAsync(new(
            "ProductPriceCreated",
            nameof(ProductPriceRecord),
            price.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { price.PriceListId, price.ProductId, price.UnitPrice, price.PackagingId })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapProductPrice(price);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            201,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<CustomerPriceGroupDto>> ListCustomerPriceGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = await dbContext.CustomerPriceGroups
            .AsNoTracking()
            .Include(x => x.PriceList)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return groups.Select(MapGroup).ToArray();
    }

    public async Task<CustomerPriceGroupDto> CreateCustomerPriceGroupAsync(
        CreateCustomerPriceGroupRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DomainException(new("PRICE_GROUP_INVALID", "Fiyat grubu kodu ve adı zorunludur."));
        }

        var idempotencyScope = $"price-group:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<CustomerPriceGroupDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var list = await dbContext.PriceLists.SingleOrDefaultAsync(x => x.Id == request.PriceListId && x.IsActive, cancellationToken);
        if (list is null)
        {
            throw new DomainException(new("PRICE_LIST_NOT_FOUND", "Aktif fiyat listesi bulunamadı."));
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.CustomerPriceGroups.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw new DomainException(new("PRICE_GROUP_CODE_TAKEN", "Bu fiyat grubu kodu zaten var."));
        }

        var group = new CustomerPriceGroupRecord
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            PriceListId = list.Id,
            IsActive = true,
        };
        dbContext.CustomerPriceGroups.Add(group);
        await auditWriter.AppendAsync(new(
            "CustomerPriceGroupCreated",
            nameof(CustomerPriceGroupRecord),
            group.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { group.Code, group.Name, group.PriceListId })));
        await dbContext.SaveChangesAsync(cancellationToken);
        group.PriceList = list;
        var result = MapGroup(group);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            201,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task AssignCustomerPriceGroupAsync(
        Guid customerId,
        AssignCustomerPriceGroupRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var idempotencyScope = $"customer-price-group:assign:{actorId}:{customerId}";
        var payloadHash = ComputePayloadHash(new { customerId, request.CustomerPriceGroupId });
        var replay = await TryReplayAsync<string>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var customerExists = await dbContext.Customers.AnyAsync(x => x.Id == customerId && !x.IsDeleted, cancellationToken);
        if (!customerExists)
        {
            throw new DomainException(new("CUSTOMER_NOT_FOUND", "Müşteri bulunamadı."));
        }

        var group = await dbContext.CustomerPriceGroups
            .Include(x => x.PriceList)
            .SingleOrDefaultAsync(x => x.Id == request.CustomerPriceGroupId && x.IsActive, cancellationToken);
        if (group is null)
        {
            throw new DomainException(new("PRICE_GROUP_NOT_FOUND", "Aktif fiyat grubu bulunamadı."));
        }

        var now = DateTimeOffset.UtcNow;
        var open = await dbContext.CustomerPriceGroupMembers
            .Where(x => x.CustomerId == customerId && (x.EffectiveTo == null || x.EffectiveTo > now))
            .ToArrayAsync(cancellationToken);
        var current = CustomerPriceResolver.SelectMembership(
            open.Select(x => new PriceGroupMembershipCandidate(x.CustomerPriceGroupId, x.EffectiveFrom, x.EffectiveTo)),
            now);
        if (current?.CustomerPriceGroupId == group.Id)
        {
            await idempotencyStore.SaveAsync(
                idempotencyScope,
                idempotencyKey,
                payloadHash,
                204,
                "\"assigned\"",
                DateTimeOffset.UtcNow.AddDays(30),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        foreach (var membership in open)
        {
            membership.EffectiveTo = now;
        }

        dbContext.CustomerPriceGroupMembers.Add(new CustomerPriceGroupMemberRecord
        {
            CustomerId = customerId,
            CustomerPriceGroupId = group.Id,
            EffectiveFrom = now,
        });
        await auditWriter.AppendAsync(new(
            "CustomerPriceGroupAssigned",
            nameof(CustomerRecord),
            customerId,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                group.Id,
                group.Code,
                group.PriceListId,
                boundToCurrentAccount = false,
            })));
        await dbContext.SaveChangesAsync(cancellationToken);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            204,
            "\"assigned\"",
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<CustomerPriceContextDto?> GetCustomerPriceContextAsync(
        Guid customerId,
        Guid? productId,
        Guid? packagingId,
        CancellationToken cancellationToken = default)
    {
        var customerExists = await dbContext.Customers.AnyAsync(
            x => x.Id == customerId && !x.IsDeleted,
            cancellationToken);
        if (!customerExists)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var memberships = await dbContext.CustomerPriceGroupMembers
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Select(x => new PriceGroupMembershipCandidate(x.CustomerPriceGroupId, x.EffectiveFrom, x.EffectiveTo))
            .ToArrayAsync(cancellationToken);
        var selected = CustomerPriceResolver.SelectMembership(memberships, now);
        if (selected is null)
        {
            return EmptyContext(customerId);
        }

        var group = await dbContext.CustomerPriceGroups
            .AsNoTracking()
            .Include(x => x.PriceList)
            .SingleOrDefaultAsync(x => x.Id == selected.CustomerPriceGroupId, cancellationToken);
        if (group is null || !group.IsActive || !group.PriceList.IsActive)
        {
            return EmptyContext(customerId);
        }

        var list = group.PriceList;
        if (list.ValidFrom > now || (list.ValidTo is { } listTo && listTo <= now))
        {
            return EmptyContext(customerId, group, list);
        }

        var query = dbContext.ProductPrices.AsNoTracking().Where(x => x.PriceListId == list.Id);
        if (productId.HasValue)
        {
            query = query.Where(x => x.ProductId == productId.Value);
        }

        var rows = await query.Take(200).ToArrayAsync(cancellationToken);
        IReadOnlyCollection<ResolvedProductPriceDto> prices;
        if (productId.HasValue)
        {
            var match = CustomerPriceResolver.SelectPrice(
                rows.Select(x => new PriceCandidate(x.ProductId, x.PackagingId, x.UnitPrice, x.CurrencyCode, x.ValidFrom, x.ValidTo)),
                productId.Value,
                packagingId,
                now);
            prices = match is null
                ? Array.Empty<ResolvedProductPriceDto>()
                : new[]
                {
                    new ResolvedProductPriceDto(
                        match.ProductId,
                        match.PackagingId,
                        match.UnitPrice,
                        match.CurrencyCode,
                        match.ValidFrom,
                        match.ValidTo),
                };
        }
        else
        {
            prices = rows
                .Where(x => x.ValidFrom <= now && (x.ValidTo is null || x.ValidTo > now))
                .Select(x => new ResolvedProductPriceDto(x.ProductId, x.PackagingId, x.UnitPrice, x.CurrencyCode, x.ValidFrom, x.ValidTo))
                .ToArray();
        }

        return new CustomerPriceContextDto(
            customerId,
            false,
            group.Id,
            group.Code,
            group.Name,
            list.Id,
            list.Code,
            list.Name,
            list.CurrencyCode,
            prices);
    }

    private static CustomerPriceContextDto EmptyContext(
        Guid customerId,
        CustomerPriceGroupRecord? group = null,
        PriceListRecord? list = null)
        => new(
            customerId,
            false,
            group?.Id,
            group?.Code,
            group?.Name,
            list?.Id,
            list?.Code,
            list?.Name,
            list?.CurrencyCode,
            Array.Empty<ResolvedProductPriceDto>());

    private async Task<T?> TryReplayAsync<T>(string scope, string key, string payloadHash, CancellationToken cancellationToken)
    {
        var stored = await idempotencyStore.FindAsync(scope, key, cancellationToken);
        if (stored is null)
        {
            return default;
        }

        if (!string.Equals(stored.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(new(
                "IDEMPOTENCY_PAYLOAD_MISMATCH",
                "Aynı Idempotency-Key farklı payload ile tekrar kullanılamaz."));
        }

        return JsonSerializer.Deserialize<T>(stored.ResponseBody);
    }

    private static string ComputePayloadHash(object payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))).ToLowerInvariant();

    private static PriceListDto MapPriceList(PriceListRecord list)
        => new(list.Id, list.Code, list.Name, list.CurrencyCode, list.ValidFrom, list.ValidTo, list.IsActive);

    private static ProductPriceDto MapProductPrice(ProductPriceRecord price)
        => new(
            price.Id,
            price.PriceListId,
            price.ProductId,
            price.PackagingId,
            price.UnitPrice,
            price.CurrencyCode,
            price.TaxCode,
            price.ValidFrom,
            price.ValidTo);

    private static CustomerPriceGroupDto MapGroup(CustomerPriceGroupRecord group)
        => new(group.Id, group.Code, group.Name, group.PriceListId, group.PriceList.Code, group.IsActive);
}
