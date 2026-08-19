using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Products;
using FactoryErp.Application.Sales;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Sales;

public sealed class SalesCommandService(
    FactoryErpDbContext dbContext,
    IProductCatalogService productCatalogService,
    ISalesPricingService pricingService,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : ISalesCommandService
{
    public async Task<QuoteRequestDto> CreatePublicQuoteRequestAsync(
        CreatePublicQuoteRequest request,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!request.ConsentAccepted)
        {
            throw new DomainException(new("CONSENT_REQUIRED", "Teklif talebi için iletişim/onay kabul edilmelidir."));
        }

        if (string.IsNullOrWhiteSpace(request.CompanyName)
            || string.IsNullOrWhiteSpace(request.ContactName)
            || string.IsNullOrWhiteSpace(request.Phone)
            || string.IsNullOrWhiteSpace(request.Email)
            || request.Items.Count == 0)
        {
            throw new DomainException(new("QUOTE_REQUEST_INVALID", "Teklif talebi firma, iletişim ve en az bir ürün içermelidir."));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var requestNumber = await NextNumberAsync("quote_request", "TLT", now, cancellationToken);
        var quoteRequest = new QuoteRequestRecord
        {
            Id = Guid.NewGuid(),
            RequestNumber = requestNumber,
            Source = "Public",
            Status = "Received",
            CustomerCandidateName = $"{request.CompanyName.Trim()} / {request.ContactName.Trim()}",
            CustomerCandidateEmail = request.Email.Trim(),
            CustomerCandidatePhone = request.Phone.Trim(),
            ConsentAt = now,
            CreatedAt = now,
        };

        foreach (var line in request.Items)
        {
            var preview = await productCatalogService.PreviewQuantityAsync(
                new QuantityPreviewRequest(line.ProductId, line.EnteredQuantity, line.EnteredPackagingId, line.ViewMode, "QuoteRequest", null),
                cancellationToken);
            if (preview is null)
            {
                throw new DomainException(new("PRODUCT_OR_PACKAGING_NOT_FOUND", "Teklif kalemindeki ürün veya ambalaj bulunamadı."));
            }

            quoteRequest.Items.Add(new QuoteRequestItemRecord
            {
                Id = Guid.NewGuid(),
                ProductId = line.ProductId,
                EnteredQuantity = line.EnteredQuantity,
                EnteredPackagingId = line.EnteredPackagingId,
                QuantityBase = preview.QuantityBase,
                PackagingSnapshot = JsonSerializer.Serialize(preview.EnteredPackaging),
            });
        }

        dbContext.QuoteRequests.Add(quoteRequest);
        await auditWriter.AppendAsync(new(
            "QuoteRequestCreated",
            nameof(QuoteRequestRecord),
            quoteRequest.Id,
            null,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { quoteRequest.RequestNumber, quoteRequest.Status, quoteRequest.Items.Count })));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapQuoteRequest(quoteRequest);
    }

    public async Task<IReadOnlyCollection<QuoteRequestDto>> ListQuoteRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.QuoteRequests
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return requests.Select(MapQuoteRequest).ToArray();
    }

    public async Task<QuoteRequestDto?> GetQuoteRequestAsync(Guid quoteRequestId, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.QuoteRequests
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == quoteRequestId, cancellationToken);
        return request is null ? null : MapQuoteRequest(request);
    }

    public async Task<IReadOnlyCollection<CustomerDto>> ListCustomersAsync(CancellationToken cancellationToken = default)
    {
        var customers = await dbContext.Customers
            .AsNoTracking()
            .Include(x => x.Contacts)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.LegalName)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var customerIds = customers.Select(x => x.Id).ToArray();
        var memberships = await dbContext.CustomerPriceGroupMembers
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.CustomerId))
            .ToArrayAsync(cancellationToken);
        var groupIds = memberships.Select(x => x.CustomerPriceGroupId).Distinct().ToArray();
        var groups = await dbContext.CustomerPriceGroups
            .AsNoTracking()
            .Where(x => groupIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return customers.Select(customer =>
        {
            var selected = CustomerPriceResolver.SelectMembership(
                memberships
                    .Where(x => x.CustomerId == customer.Id)
                    .Select(x => new PriceGroupMembershipCandidate(x.CustomerPriceGroupId, x.EffectiveFrom, x.EffectiveTo)),
                now);
            groups.TryGetValue(selected?.CustomerPriceGroupId ?? Guid.Empty, out var group);
            var primary = customer.Contacts
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.IsPrimary)
                .FirstOrDefault();
            return MapCustomer(customer, primary?.FullName, group?.Code, group?.Name);
        }).ToArray();
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == customerId && !x.IsDeleted, cancellationToken);
        return customer is null ? null : MapCustomer(customer);
    }

    public async Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LegalName))
        {
            throw new DomainException(new("CUSTOMER_INVALID", "Müşteri unvanı zorunludur."));
        }

        var idempotencyScope = $"customer:create:{actorId}";
        var payloadHash = ComputePayloadHash(new
        {
            request.LegalName,
            request.Email,
            request.Phone,
            request.TaxNumber,
            request.TaxOffice,
            action = "create",
        });
        var replay = await TryReplayAsync<CustomerDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var customer = new CustomerRecord
        {
            Id = Guid.NewGuid(),
            CustomerCode = await NextNumberAsync("customer", "MUS", now, cancellationToken),
            LegalName = request.LegalName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            TaxNumber = string.IsNullOrWhiteSpace(request.TaxNumber) ? null : request.TaxNumber.Trim(),
            TaxOffice = string.IsNullOrWhiteSpace(request.TaxOffice) ? null : request.TaxOffice.Trim(),
            Status = "Active",
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.Customers.Add(customer);
        await auditWriter.AppendAsync(new(
            "CustomerCreated",
            nameof(CustomerRecord),
            customer.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { customer.CustomerCode, customer.LegalName, customer.Status })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapCustomer(customer);
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

    public async Task<QuoteRequestDto?> ReviewQuoteRequestAsync(
        Guid quoteRequestId,
        Guid actorId,
        Guid? customerId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var idempotencyScope = $"quote-request:review:{actorId}:{quoteRequestId}";
        var payloadHash = ComputePayloadHash(new { quoteRequestId, actorId, customerId, action = "review" });
        var replay = await TryReplayAsync<QuoteRequestDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var request = await dbContext.QuoteRequests
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == quoteRequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        if (request.Status is not ("Received" or "InReview"))
        {
            throw new DomainException(new("QUOTE_REQUEST_NOT_REVIEWABLE", "Teklif talebi mevcut durumunda incelenemez."));
        }

        if (customerId.HasValue)
        {
            var customerExists = await dbContext.Customers.AnyAsync(
                x => x.Id == customerId.Value && !x.IsDeleted && x.Status == "Active",
                cancellationToken);
            if (!customerExists)
            {
                throw new DomainException(new("CUSTOMER_NOT_ACTIVE", "Teklif talebi yalnızca aktif müşteriyle bağlanabilir."));
            }
        }

        request.CustomerId = customerId;
        request.Status = "InReview";
        request.ReviewedBy = actorId;
        request.ReviewedAt = DateTimeOffset.UtcNow;
        await auditWriter.AppendAsync(new("QuoteRequestReviewed", nameof(QuoteRequestRecord), request.Id, actorId, correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapQuoteRequest(request);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<SalesOrderDto> CreateSalesOrderAsync(
        CreateSalesOrderRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(x => x.Id == request.CustomerId && !x.IsDeleted && x.Status == "Active", cancellationToken);
        if (customer is null)
        {
            throw new DomainException(new("CUSTOMER_NOT_ACTIVE", "Sipariş yalnızca aktif müşteri için oluşturulabilir."));
        }

        if (request.Items.Count == 0)
        {
            throw new DomainException(new("ORDER_ITEMS_REQUIRED", "Sipariş en az bir kalem içermelidir."));
        }

        var idempotencyScope = $"sales-order:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<SalesOrderDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var order = new SalesOrderRecord
        {
            Id = Guid.NewGuid(),
            OrderNumber = await NextNumberAsync("sales_order", "SO", now, cancellationToken),
            CustomerId = request.CustomerId,
            Status = "Draft",
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "TRY" : request.CurrencyCode.Trim().ToUpperInvariant(),
            CreatedBy = actorId,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        };

        foreach (var line in request.Items)
        {
            var preview = await productCatalogService.PreviewQuantityAsync(
                new QuantityPreviewRequest(line.ProductId, line.EnteredQuantity, line.EnteredPackagingId, line.ViewMode, "SalesOrder", null),
                cancellationToken);
            if (preview is null)
            {
                throw new DomainException(new("PRODUCT_OR_PACKAGING_NOT_FOUND", "Sipariş kalemindeki ürün veya ambalaj bulunamadı."));
            }

            var price = line.UnitPrice < 0 ? throw new DomainException(new("PRICE_INVALID", "Birim fiyat negatif olamaz.")) : line.UnitPrice;
            order.Items.Add(new SalesOrderItemRecord
            {
                Id = Guid.NewGuid(),
                ProductId = line.ProductId,
                OrderedQty = preview.QuantityBase,
                ReservedQty = 0,
                ShippedQty = 0,
                CancelledQty = 0,
                RemainingQty = preview.QuantityBase,
                EnteredQuantity = line.EnteredQuantity,
                EnteredPackagingId = line.EnteredPackagingId,
                PackagingSnapshot = JsonSerializer.Serialize(preview.EnteredPackaging),
                PartialDeliveryAllowed = line.PartialDeliveryAllowed,
                UnitPrice = price,
                TaxCode = line.TaxCode,
                PriceSnapshot = JsonSerializer.Serialize(new { unitPrice = price, currency = order.CurrencyCode, at = now }),
                RowVersion = 1,
            });
        }

        order.TotalNet = order.Items.Sum(x => x.OrderedQty * x.UnitPrice);
        order.TotalTax = 0;
        order.TotalGross = order.TotalNet + order.TotalTax;
        dbContext.SalesOrders.Add(order);
        await auditWriter.AppendAsync(new(
            "SalesOrderCreated",
            nameof(SalesOrderRecord),
            order.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { order.OrderNumber, order.Status, order.Items.Count })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapSalesOrder(order);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    public async Task<IReadOnlyCollection<SalesOrderDto>> ListSalesOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.SalesOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return await MapSalesOrdersAsync(orders, cancellationToken);
    }

    public async Task<SalesOrderDto?> GetSalesOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var mapped = await MapSalesOrdersAsync(new[] { order }, cancellationToken);
        return mapped[0];
    }

    public async Task<SalesOrderDto?> SubmitSalesOrderAsync(
        Guid orderId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var idempotencyScope = $"sales-order:submit:{actorId}:{orderId}";
        var payloadHash = ComputePayloadHash(new { orderId, actorId, action = "submit" });
        var replay = await TryReplayAsync<SalesOrderDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var order = await dbContext.SalesOrders
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.Status != "Draft" || order.Items.Count == 0)
        {
            throw new DomainException(new("INVALID_ORDER_SUBMISSION", "Yalnızca kalem içeren taslak sipariş onaya gönderilebilir."));
        }

        order.Status = "PendingApproval";
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await auditWriter.AppendAsync(new("SalesOrderSubmitted", nameof(SalesOrderRecord), order.Id, actorId, correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapSalesOrder(order);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<SalesOrderDto?> ApproveSalesOrderAsync(
        Guid orderId,
        Guid actorId,
        string? comment,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var idempotencyScope = $"sales-order:approve:{actorId}:{orderId}";
        var payloadHash = ComputePayloadHash(new { orderId, actorId, comment, action = "approve" });
        var replay = await TryReplayAsync<SalesOrderDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var order = await dbContext.SalesOrders
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.Status != "PendingApproval")
        {
            throw new DomainException(new("STATE_TRANSITION_CONFLICT", "Sipariş yalnızca onay beklerken onaylanabilir."));
        }

        var warehouse = await dbContext.Warehouses
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);
        if (warehouse is null)
        {
            throw new DomainException(new("WAREHOUSE_REQUIRED", "Rezervasyon için aktif depo bulunamadı."));
        }

        foreach (var item in order.Items)
        {
            var stock = await LockFirstStockAsync(item.ProductId, warehouse.Id, cancellationToken);
            if (stock is null)
            {
                throw new DomainException(new("STOCK_NOT_FOUND", "Sipariş ürünü için depo stoğu bulunamadı."));
            }

            var available = stock.OnHandQtyBase - stock.ReservedQtyBase;
            if (available < item.RemainingQty)
            {
                throw new DomainException(new(
                    "INSUFFICIENT_AVAILABLE_STOCK",
                    "Sipariş onayı için kullanılabilir stok yetersiz.",
                    new Dictionary<string, object?>
                    {
                        ["productId"] = item.ProductId,
                        ["requestedQuantityBase"] = item.RemainingQty,
                        ["availableQuantityBase"] = available,
                    }));
            }

            stock.ReservedQtyBase += item.RemainingQty;
            item.ReservedQty += item.RemainingQty;
            dbContext.StockReservations.Add(new StockReservationRecord
            {
                Id = Guid.NewGuid(),
                SalesOrderItemId = item.Id,
                ProductId = item.ProductId,
                WarehouseId = warehouse.Id,
                QuantityBase = item.RemainingQty,
                ConsumedQtyBase = 0,
                ReleasedQtyBase = 0,
                Status = "Open",
                RowVersion = 1,
            });
        }

        order.Status = "Approved";
        order.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.SalesOrderApprovals.Add(new SalesOrderApprovalRecord
        {
            Id = Guid.NewGuid(),
            SalesOrderId = order.Id,
            Decision = "Approved",
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            DecidedBy = actorId,
            DecidedAt = DateTimeOffset.UtcNow,
        });
        await auditWriter.AppendAsync(new(
            "SalesOrderApproved",
            nameof(SalesOrderRecord),
            order.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { order.Status, reservation = true })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapSalesOrder(order);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<SalesOrderDto?> RejectSalesOrderAsync(
        Guid orderId,
        Guid actorId,
        string comment,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainException(new("REJECTION_COMMENT_REQUIRED", "Sipariş reddi için gerekçe zorunludur."));
        }

        var idempotencyScope = $"sales-order:reject:{actorId}:{orderId}";
        var payloadHash = ComputePayloadHash(new { orderId, actorId, comment, action = "reject" });
        var replay = await TryReplayAsync<SalesOrderDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var order = await dbContext.SalesOrders
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.Status != "PendingApproval")
        {
            throw new DomainException(new("STATE_TRANSITION_CONFLICT", "Sipariş yalnızca onay beklerken reddedilebilir."));
        }

        order.Status = "Cancelled";
        order.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.SalesOrderApprovals.Add(new SalesOrderApprovalRecord
        {
            Id = Guid.NewGuid(),
            SalesOrderId = order.Id,
            Decision = "Rejected",
            Comment = comment.Trim(),
            DecidedBy = actorId,
            DecidedAt = DateTimeOffset.UtcNow,
        });
                await auditWriter.AppendAsync(new("SalesOrderRejected", nameof(SalesOrderRecord), order.Id, actorId, correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapSalesOrder(order);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<QuoteDto>> ListQuotesAsync(CancellationToken cancellationToken = default)
    {
        var quotes = await dbContext.Quotes
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return await MapQuotesAsync(quotes, cancellationToken);
    }

    public async Task<QuoteDto?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.Quotes
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == quoteId, cancellationToken);
        if (quote is null)
        {
            return null;
        }

        var mapped = await MapQuotesAsync(new[] { quote }, cancellationToken);
        return mapped[0];
    }

    public async Task<QuoteDto> CreateQuoteAsync(
        CreateQuoteRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new DomainException(new("QUOTE_ITEMS_REQUIRED", "Teklif en az bir kalem içermelidir."));
        }

        if (request.Items.GroupBy(x => x.QuoteRequestItemId).Any(group => group.Count() > 1))
        {
            throw new DomainException(new("QUOTE_ITEM_DUPLICATE", "Aynı talep kalemi birden fazla fiyatlanamaz."));
        }

        foreach (var line in request.Items)
        {
            if (line.UnitPrice < 0)
            {
                throw new DomainException(new("PRICE_INVALID", "Birim fiyat negatif olamaz."));
            }
        }

        var idempotencyScope = $"quote:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<QuoteDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var quoteRequest = await dbContext.QuoteRequests
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == request.QuoteRequestId, cancellationToken);
        if (quoteRequest is null)
        {
            throw new DomainException(new("QUOTE_REQUEST_NOT_FOUND", "Teklif talebi bulunamadı."));
        }

        if (quoteRequest.Status != "InReview")
        {
            throw new DomainException(new("QUOTE_REQUEST_NOT_CONVERTIBLE", "Teklif yalnızca incelemedeki talepten oluşturulur."));
        }

        if (!quoteRequest.CustomerId.HasValue)
        {
            throw new DomainException(new("QUOTE_CUSTOMER_REQUIRED", "Teklif için talep aktif müşteriye bağlı olmalıdır."));
        }

        var customer = await dbContext.Customers.SingleOrDefaultAsync(
            x => x.Id == quoteRequest.CustomerId.Value && !x.IsDeleted && x.Status == "Active",
            cancellationToken);
        if (customer is null)
        {
            throw new DomainException(new("CUSTOMER_NOT_ACTIVE", "Teklif yalnızca aktif müşteri için oluşturulabilir."));
        }

        var alreadyConverted = await dbContext.Quotes.AnyAsync(
            x => x.QuoteRequestId == quoteRequest.Id,
            cancellationToken);
        if (alreadyConverted || quoteRequest.Status == "Converted")
        {
            throw new DomainException(new("QUOTE_REQUEST_ALREADY_CONVERTED", "Bu teklif talebi için zaten teklif belgesi var."));
        }

        var requestItemIds = quoteRequest.Items.Select(x => x.Id).ToHashSet();
        var pricedIds = request.Items.Select(x => x.QuoteRequestItemId).ToHashSet();
        if (!requestItemIds.SetEquals(pricedIds))
        {
            throw new DomainException(new(
                "QUOTE_ITEMS_MUST_MATCH_REQUEST",
                "Teklif kalemleri talep satırlarıyla birebir eşleşmelidir."));
        }

        var now = DateTimeOffset.UtcNow;
        if (request.ValidUntil is { } validUntil && validUntil <= now)
        {
            throw new DomainException(new("QUOTE_VALID_UNTIL_INVALID", "Geçerlilik tarihi gelecekte olmalıdır."));
        }

        var quote = new QuoteRecord
        {
            Id = Guid.NewGuid(),
            QuoteNumber = await NextNumberAsync("quote", "TEK", now, cancellationToken),
            Status = "Draft",
            CustomerId = customer.Id,
            QuoteRequestId = quoteRequest.Id,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode)
                ? "TRY"
                : request.CurrencyCode.Trim().ToUpperInvariant(),
            ValidUntil = request.ValidUntil,
            CreatedBy = actorId,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        };

        var priceContext = await pricingService.GetCustomerPriceContextAsync(customer.Id, null, null, cancellationToken);
        var listCandidates = (priceContext?.Prices ?? Array.Empty<ResolvedProductPriceDto>())
            .Select(x => new PriceCandidate(x.ProductId, x.PackagingId, x.UnitPrice, x.CurrencyCode, x.ValidFrom, x.ValidTo))
            .ToArray();
        var requestItemsById = quoteRequest.Items.ToDictionary(x => x.Id);
        foreach (var line in request.Items)
        {
            var requestItem = requestItemsById[line.QuoteRequestItemId];
            var preview = await productCatalogService.PreviewQuantityAsync(
                new QuantityPreviewRequest(
                    requestItem.ProductId,
                    requestItem.EnteredQuantity,
                    requestItem.EnteredPackagingId,
                    line.ViewMode,
                    "Quote",
                    null),
                cancellationToken);
            if (preview is null)
            {
                throw new DomainException(new("PRODUCT_OR_PACKAGING_NOT_FOUND", "Teklif kalemindeki ürün veya ambalaj bulunamadı."));
            }

            var listPrice = CustomerPriceResolver.SelectPrice(
                listCandidates,
                requestItem.ProductId,
                requestItem.EnteredPackagingId,
                now);
            var lineNet = decimal.Round(preview.QuantityBase * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
            quote.Items.Add(new QuoteItemRecord
            {
                Id = Guid.NewGuid(),
                ProductId = requestItem.ProductId,
                QuoteRequestItemId = requestItem.Id,
                EnteredQuantity = requestItem.EnteredQuantity,
                EnteredPackagingId = requestItem.EnteredPackagingId,
                QuantityBase = preview.QuantityBase,
                PackagingSnapshot = JsonSerializer.Serialize(preview.EnteredPackaging),
                UnitPrice = line.UnitPrice,
                ListUnitPrice = listPrice?.UnitPrice,
                PriceListId = priceContext?.PriceListId,
                TaxCode = string.IsNullOrWhiteSpace(line.TaxCode) ? null : line.TaxCode.Trim(),
                PriceSnapshot = JsonSerializer.Serialize(new
                {
                    unitPrice = line.UnitPrice,
                    listUnitPrice = listPrice?.UnitPrice,
                    priceListId = priceContext?.PriceListId,
                    priceListCode = priceContext?.PriceListCode,
                    customerPriceGroupId = priceContext?.CustomerPriceGroupId,
                    overridden = listPrice is not null && listPrice.UnitPrice != line.UnitPrice,
                    boundToCurrentAccount = false,
                    currency = quote.CurrencyCode,
                    at = now,
                }),
                LineNet = lineNet,
                RowVersion = 1,
            });
        }

        quote.TotalNet = quote.Items.Sum(x => x.LineNet);
        quote.TotalTax = 0;
        quote.TotalGross = quote.TotalNet + quote.TotalTax;
        quoteRequest.Status = "Converted";
        dbContext.Quotes.Add(quote);
        await auditWriter.AppendAsync(new(
            "QuoteCreated",
            nameof(QuoteRecord),
            quote.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                quote.QuoteNumber,
                quote.Status,
                quote.QuoteRequestId,
                quote.Items.Count,
                quote.TotalNet,
            })));
        await auditWriter.AppendAsync(new(
            "QuoteRequestConverted",
            nameof(QuoteRequestRecord),
            quoteRequest.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { quoteRequest.Status, quoteId = quote.Id })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapQuote(quote, customer.CustomerCode, customer.LegalName);
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

    public async Task<QuoteDto?> IssueQuoteAsync(
        Guid quoteId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var idempotencyScope = $"quote:issue:{actorId}:{quoteId}";
        var payloadHash = ComputePayloadHash(new { quoteId, actorId, action = "issue" });
        var replay = await TryReplayAsync<QuoteDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var quote = await dbContext.Quotes
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == quoteId, cancellationToken);
        if (quote is null)
        {
            return null;
        }

        if (quote.Status != "Draft")
        {
            throw new DomainException(new("STATE_TRANSITION_CONFLICT", "Teklif yalnızca taslakken kesinleştirilebilir."));
        }

        if (quote.Items.Count == 0)
        {
            throw new DomainException(new("QUOTE_ITEMS_REQUIRED", "Teklif en az bir kalem içermelidir."));
        }

        var now = DateTimeOffset.UtcNow;
        quote.Status = "Issued";
        quote.IssuedAt = now;
        quote.IssuedBy = actorId;
        quote.UpdatedAt = now;
        await auditWriter.AppendAsync(new(
            "QuoteIssued",
            nameof(QuoteRecord),
            quote.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { quote.QuoteNumber, quote.Status, quote.IssuedAt })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var customer = await dbContext.Customers.SingleAsync(x => x.Id == quote.CustomerId, cancellationToken);
        var result = MapQuote(quote, customer.CustomerCode, customer.LegalName);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<T?> TryReplayAsync<T>(
        string scope,
        string key,
        string payloadHash,
        CancellationToken cancellationToken)
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
    {
        var json = JsonSerializer.Serialize(payload);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private async Task<StockRecord?> LockFirstStockAsync
(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
        => await dbContext.Stocks
            .FromSqlInterpolated($"SELECT * FROM stocks WHERE product_id = {productId} AND warehouse_id = {warehouseId} ORDER BY location_id LIMIT 1 FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<string> NextNumberAsync(string documentType, string prefix, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var year = now.Year;
        var sequence = await dbContext.DocumentSequences
            .FromSqlInterpolated($"SELECT * FROM document_sequences WHERE document_type = {documentType} AND year = {year} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (sequence is null)
        {
            sequence = new DocumentSequenceRecord
            {
                Id = Guid.NewGuid(),
                DocumentType = documentType,
                Year = year,
                CurrentValue = 1,
                UpdatedAt = now,
            };
            dbContext.DocumentSequences.Add(sequence);
        }
        else
        {
            sequence.CurrentValue++;
            sequence.UpdatedAt = now;
        }

        return $"{prefix}-{year}-{sequence.CurrentValue:D6}";
    }

    private static QuoteRequestDto MapQuoteRequest(QuoteRequestRecord request)
        => new(
            request.Id,
            request.RequestNumber,
            request.Status,
            request.Source,
            request.CustomerCandidateName ?? string.Empty,
            request.CustomerCandidateEmail ?? string.Empty,
            request.CustomerCandidatePhone ?? string.Empty,
            request.Items.Select(x => new QuoteRequestItemDto(
                x.Id,
                x.ProductId,
                x.EnteredQuantity,
                x.EnteredPackagingId,
                x.QuantityBase,
                x.PackagingSnapshot)).ToArray(),
            request.CreatedAt,
            request.CustomerId);

    private static CustomerDto MapCustomer(
        CustomerRecord customer,
        string? primaryContactName = null,
        string? priceGroupCode = null,
        string? priceGroupName = null)
        => new(
            customer.Id,
            customer.CustomerCode,
            customer.LegalName,
            customer.Status,
            customer.Email,
            customer.Phone,
            customer.CreatedAt,
            primaryContactName,
            priceGroupCode,
            priceGroupName);

    private async Task<IReadOnlyList<SalesOrderDto>> MapSalesOrdersAsync(
        IReadOnlyCollection<SalesOrderRecord> orders,
        CancellationToken cancellationToken)
    {
        if (orders.Count == 0)
        {
            return Array.Empty<SalesOrderDto>();
        }

        var customerIds = orders.Select(x => x.CustomerId).Distinct().ToArray();
        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return orders.Select(order =>
        {
            customers.TryGetValue(order.CustomerId, out var customer);
            return MapSalesOrder(order, customer?.CustomerCode, customer?.LegalName);
        }).ToArray();
    }

    private static SalesOrderDto MapSalesOrder(
        SalesOrderRecord order,
        string? customerCode = null,
        string? customerLegalName = null)
        => new(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Status,
            order.CurrencyCode,
            order.TotalNet,
            order.TotalTax,
            order.TotalGross,
            order.RowVersion,
            order.Items.Select(x => new SalesOrderItemDto(
                x.Id,
                x.ProductId,
                x.OrderedQty,
                x.ReservedQty,
                x.ShippedQty,
                x.CancelledQty,
                x.RemainingQty,
                x.EnteredQuantity,
                x.EnteredPackagingId,
                x.PackagingSnapshot,
                x.PartialDeliveryAllowed,
                x.UnitPrice,
                x.TaxCode,
                x.RowVersion)).ToArray(),
            order.CreatedAt,
            customerCode,
            customerLegalName);

    private async Task<IReadOnlyList<QuoteDto>> MapQuotesAsync(
        IReadOnlyCollection<QuoteRecord> quotes,
        CancellationToken cancellationToken)
    {
        if (quotes.Count == 0)
        {
            return Array.Empty<QuoteDto>();
        }

        var customerIds = quotes.Select(x => x.CustomerId).Distinct().ToArray();
        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return quotes.Select(quote =>
        {
            customers.TryGetValue(quote.CustomerId, out var customer);
            return MapQuote(
                quote,
                customer?.CustomerCode ?? string.Empty,
                customer?.LegalName ?? string.Empty);
        }).ToArray();
    }

    private static QuoteDto MapQuote(QuoteRecord quote, string customerCode, string customerLegalName)
        => new(
            quote.Id,
            quote.QuoteNumber,
            quote.Status,
            quote.CustomerId,
            customerCode,
            customerLegalName,
            quote.QuoteRequestId,
            quote.CurrencyCode,
            quote.TotalNet,
            quote.TotalTax,
            quote.TotalGross,
            quote.ValidUntil,
            quote.IssuedAt,
            quote.IssuedBy,
            quote.RowVersion,
            quote.Items.Select(x => new QuoteItemDto(
                x.Id,
                x.ProductId,
                x.QuoteRequestItemId,
                x.EnteredQuantity,
                x.EnteredPackagingId,
                x.QuantityBase,
                x.PackagingSnapshot,
                x.UnitPrice,
                x.ListUnitPrice,
                x.PriceListId,
                x.TaxCode,
                x.LineNet,
                x.RowVersion)).ToArray(),
            quote.CreatedAt);
}
