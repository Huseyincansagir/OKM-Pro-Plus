using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Products;
using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Shipping;

public sealed class DeliveryInvoiceFinanceService(
    FactoryErpDbContext dbContext,
    IProductCatalogService productCatalogService,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IShippingFinanceCommandService
{
    public async Task<DeliveryNoteDto> CreateDeliveryNoteAsync(
        CreateDeliveryNoteRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new DomainException(new("DELIVERY_ITEMS_REQUIRED", "İrsaliye en az bir kalem içermelidir."));
        }

        var scope = $"delivery-note:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<DeliveryNoteDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var order = await dbContext.SalesOrders
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == request.SalesOrderId, cancellationToken);
        if (order is null)
        {
            throw new DomainException(new("ORDER_NOT_FOUND", "İrsaliye için satış siparişi bulunamadı."));
        }

        if (order.Status is not ("Approved" or "Preparing" or "PartiallyShipped"))
        {
            throw new DomainException(new("ORDER_NOT_SHIPPABLE", "Sipariş mevcut durumunda sevkiyata hazırlanamaz."));
        }

        var now = DateTimeOffset.UtcNow;
        var note = new DeliveryNoteRecord
        {
            Id = Guid.NewGuid(),
            DocumentNumber = await NextNumberAsync("delivery_note", "DN", now, cancellationToken),
            SalesOrderId = order.Id,
            CustomerId = order.CustomerId,
            Status = "Draft",
            CreatedAt = now,
            RowVersion = 1,
        };

        foreach (var input in request.Items)
        {
            var orderItem = order.Items.SingleOrDefault(x => x.Id == input.SalesOrderItemId);
            if (orderItem is null)
            {
                throw new DomainException(new("ORDER_ITEM_NOT_FOUND", "İrsaliye kalemi sipariş kaleminde bulunamadı."));
            }

            var remainingOrderQuantity = orderItem.OrderedQty - orderItem.ShippedQty - orderItem.CancelledQty;
            var preview = await productCatalogService.PreviewQuantityAsync(
                new QuantityPreviewRequest(orderItem.ProductId, input.EnteredQuantity, input.EnteredPackagingId, input.ViewMode, "DeliveryNote", null),
                cancellationToken);
            if (preview is null)
            {
                throw new DomainException(new("PRODUCT_OR_PACKAGING_NOT_FOUND", "İrsaliye kalemindeki ürün veya ambalaj bulunamadı."));
            }

            if (preview.QuantityBase > remainingOrderQuantity)
            {
                throw new DomainException(new(
                    "OVER_SHIPMENT",
                    "İrsaliye miktarı siparişin kalan miktarını aşamaz.",
                    new Dictionary<string, object?>
                    {
                        ["requestedQuantityBase"] = preview.QuantityBase,
                        ["remainingOrderQuantityBase"] = remainingOrderQuantity,
                    }));
            }

            note.Items.Add(new DeliveryNoteItemRecord
            {
                Id = Guid.NewGuid(),
                SalesOrderItemId = orderItem.Id,
                ProductId = orderItem.ProductId,
                QuantityBase = preview.QuantityBase,
                EnteredQuantity = input.EnteredQuantity,
                EnteredPackagingId = input.EnteredPackagingId,
                PackagingSnapshot = JsonSerializer.Serialize(preview.EnteredPackaging),
                ShippedQty = 0,
                InvoicedQty = 0,
                WaivedQty = 0,
                RemainingToInvoice = 0,
                RowVersion = 1,
            });
        }

        dbContext.DeliveryNotes.Add(note);
        await auditWriter.AppendAsync(new(
            "DeliveryNoteCreated",
            nameof(DeliveryNoteRecord),
            note.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { note.DocumentNumber, note.Status, note.Items.Count })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapDeliveryNote(note);
        await idempotencyStore.SaveAsync(scope, idempotencyKey, payloadHash, 200, JsonSerializer.Serialize(result), now.AddDays(30), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<DeliveryNoteDto?> GetDeliveryNoteAsync(Guid deliveryNoteId, CancellationToken cancellationToken = default)
    {
        var note = await dbContext.DeliveryNotes.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == deliveryNoteId, cancellationToken);
        return note is null ? null : MapDeliveryNote(note);
    }

    public async Task<DeliveryNoteDto?> IssueDeliveryNoteAsync(
        Guid deliveryNoteId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"delivery-note:issue:{actorId}:{deliveryNoteId}";
        var payloadHash = ComputePayloadHash(new { deliveryNoteId, actorId, action = "issue" });
        var replay = await TryReplayAsync<DeliveryNoteDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var note = await dbContext.DeliveryNotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == deliveryNoteId, cancellationToken);
        if (note is null)
        {
            return null;
        }

        if (note.Status is not ("Draft" or "Prepared" or "ReadyToIssue"))
        {
            throw new DomainException(new("DELIVERY_NOTE_NOT_ISSUABLE", "İrsaliye mevcut durumunda düzenlenemez."));
        }

        var order = await dbContext.SalesOrders.SingleAsync(x => x.Id == note.SalesOrderId, cancellationToken);
        foreach (var item in note.Items)
        {
            var orderItem = await LockSalesOrderItemAsync(item.SalesOrderItemId, cancellationToken);
            if (orderItem is null)
            {
                throw new DomainException(new("ORDER_ITEM_NOT_FOUND", "İrsaliyenin sipariş kalemi bulunamadı."));
            }

            var shipmentRemaining = orderItem.ReservedQty - orderItem.ShippedQty - orderItem.CancelledQty;
            if (item.QuantityBase > shipmentRemaining)
            {
                throw new DomainException(new(
                    "RESERVATION_SHIPMENT_CONFLICT",
                    "İrsaliye miktarı aktif rezervasyonun kalan miktarını aşamaz.",
                    new Dictionary<string, object?>
                    {
                        ["deliveryQuantityBase"] = item.QuantityBase,
                        ["reservedRemainingBase"] = shipmentRemaining,
                    }));
            }

            var reservation = await dbContext.StockReservations
                .FromSqlInterpolated($"SELECT * FROM stock_reservations WHERE sales_order_item_id = {item.SalesOrderItemId} AND status IN ('Open', 'PartiallyConsumed') ORDER BY id LIMIT 1 FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (reservation is null)
            {
                throw new DomainException(new("RESERVATION_NOT_FOUND", "İrsaliye kalemi için aktif stok rezervasyonu bulunamadı."));
            }

            var reservationRemaining = reservation.QuantityBase - reservation.ConsumedQtyBase - reservation.ReleasedQtyBase;
            if (item.QuantityBase > reservationRemaining)
            {
                throw new DomainException(new("RESERVATION_SHIPMENT_CONFLICT", "İrsaliye miktarı rezervasyon kalanını aşamaz."));
            }

            var stock = await dbContext.Stocks
                .FromSqlInterpolated($"SELECT * FROM stocks WHERE product_id = {item.ProductId} AND warehouse_id = {reservation.WarehouseId} ORDER BY location_id LIMIT 1 FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (stock is null || stock.OnHandQtyBase < item.QuantityBase || stock.ReservedQtyBase < item.QuantityBase)
            {
                throw new DomainException(new("STOCK_ISSUE_CONFLICT", "Sevkiyat için kullanılabilir fiziksel veya rezerve stok yetersiz."));
            }

            stock.OnHandQtyBase -= item.QuantityBase;
            stock.ReservedQtyBase -= item.QuantityBase;
            reservation.ConsumedQtyBase += item.QuantityBase;
            reservation.Status = reservation.ConsumedQtyBase + reservation.ReleasedQtyBase >= reservation.QuantityBase
                ? "Consumed"
                : "PartiallyConsumed";
            orderItem.ShippedQty += item.QuantityBase;
            orderItem.RemainingQty = orderItem.OrderedQty - orderItem.ShippedQty - orderItem.CancelledQty;
            item.ShippedQty = item.QuantityBase;
            item.RemainingToInvoice = item.ShippedQty - item.InvoicedQty - item.WaivedQty;

            dbContext.StockMovements.Add(new StockMovementRecord
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                WarehouseId = reservation.WarehouseId,
                LocationId = stock.LocationId,
                MovementType = "DeliveryIssue",
                QuantityBase = item.QuantityBase,
                SourceEntityType = nameof(DeliveryNoteRecord),
                SourceEntityId = note.Id,
                PackagingSnapshot = item.PackagingSnapshot,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            dbContext.DeliveryNoteItemAllocations.Add(new DeliveryNoteItemAllocationRecord
            {
                Id = Guid.NewGuid(),
                SalesOrderItemId = item.SalesOrderItemId,
                DeliveryNoteItemId = item.Id,
                QuantityBase = item.QuantityBase,
                BaseUomId = await ProductBaseUomIdAsync(item.ProductId, cancellationToken),
                PackagingSnapshot = item.PackagingSnapshot,
                AllocationKind = "Original",
                Status = "Active",
                IdempotencyKey = $"{idempotencyKey}:{item.Id}",
                PayloadHash = payloadHash,
                CreatedBy = actorId,
                CreatedAt = DateTimeOffset.UtcNow,
                RowVersion = 1,
            });
        }

        var allFulfilled = await dbContext.SalesOrderItems
            .Where(x => x.SalesOrderId == order.Id)
            .AllAsync(x => x.ShippedQty + x.CancelledQty >= x.OrderedQty, cancellationToken);
        order.Status = allFulfilled ? "Fulfilled" : "PartiallyShipped";
        note.Status = "Issued";
        note.IssuedAt = DateTimeOffset.UtcNow;
        note.IssuedBy = actorId;
        await auditWriter.AppendAsync(new("DeliveryNoteIssued", nameof(DeliveryNoteRecord), note.Id, actorId, correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapDeliveryNote(note);
        await idempotencyStore.SaveAsync(scope, idempotencyKey, payloadHash, 200, JsonSerializer.Serialize(result), DateTimeOffset.UtcNow.AddDays(30), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(
        CreateInvoiceRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new DomainException(new("INVOICE_ITEMS_REQUIRED", "Fatura en az bir kalem içermelidir."));
        }

        var scope = $"invoice:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<InvoiceDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (!await dbContext.Customers.AnyAsync(x => x.Id == request.CustomerId && x.Status == "Active" && !x.IsDeleted, cancellationToken))
        {
            throw new DomainException(new("CUSTOMER_NOT_ACTIVE", "Fatura yalnızca aktif müşteri için oluşturulabilir."));
        }

        var invoice = new InvoiceRecord
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = await NextNumberAsync("invoice", "INV", DateTimeOffset.UtcNow, cancellationToken),
            CustomerId = request.CustomerId,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "TRY" : request.CurrencyCode.Trim().ToUpperInvariant(),
            Status = "Draft",
            TaxSnapshot = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            RowVersion = 1,
        };

        foreach (var input in request.Items)
        {
            var deliveryItem = await dbContext.DeliveryNoteItems
                .Include(x => x.DeliveryNote)
                .SingleOrDefaultAsync(x => x.Id == input.DeliveryNoteItemId, cancellationToken);
            if (deliveryItem is null || deliveryItem.DeliveryNote.Status != "Issued" || deliveryItem.DeliveryNote.CustomerId != request.CustomerId)
            {
                throw new DomainException(new("INVOICE_SOURCE_NOT_ISSUED", "Fatura kalemi issued bir irsaliyeden gelmelidir."));
            }

            var preview = await productCatalogService.PreviewQuantityAsync(
                new QuantityPreviewRequest(deliveryItem.ProductId, input.EnteredQuantity, input.EnteredPackagingId, input.ViewMode, "Invoice", null),
                cancellationToken);
            if (preview is null || preview.QuantityBase > deliveryItem.RemainingToInvoice)
            {
                throw new DomainException(new("OVER_INVOICING", "Fatura miktarı irsaliyenin kalan faturalanabilir miktarını aşamaz."));
            }

            var tax = input.TaxCodeId.HasValue
                ? await dbContext.TaxCodes.SingleOrDefaultAsync(x => x.Id == input.TaxCodeId.Value && x.IsActive, cancellationToken)
                : null;
            if (input.TaxCodeId.HasValue && tax is null)
            {
                throw new DomainException(new("TAX_CODE_NOT_FOUND", "Aktif vergi kodu bulunamadı."));
            }

            var lineTotal = decimal.Round(preview.QuantityBase * input.UnitPrice, 2, MidpointRounding.AwayFromZero);
            invoice.Items.Add(new InvoiceItemRecord
            {
                Id = Guid.NewGuid(),
                DeliveryNoteItemId = deliveryItem.Id,
                ProductId = deliveryItem.ProductId,
                QuantityBase = preview.QuantityBase,
                EnteredQuantity = input.EnteredQuantity,
                EnteredPackagingId = input.EnteredPackagingId,
                PackagingSnapshot = JsonSerializer.Serialize(preview.EnteredPackaging),
                UnitPrice = input.UnitPrice,
                TaxCodeId = input.TaxCodeId,
                TaxSnapshot = JsonSerializer.Serialize(new { code = tax?.Code, rate = tax?.Rate ?? 0m }),
                LineTotal = lineTotal,
            });
        }

        invoice.Subtotal = invoice.Items.Sum(x => x.LineTotal);
        invoice.TaxTotal = invoice.Items.Sum(x => x.LineTotal * TaxRateFromSnapshot(x.TaxSnapshot));
        invoice.GrandTotal = invoice.Subtotal + invoice.TaxTotal;
        dbContext.Invoices.Add(invoice);
        await auditWriter.AppendAsync(new("InvoiceCreated", nameof(InvoiceRecord), invoice.Id, actorId, correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapInvoice(invoice);
        await idempotencyStore.SaveAsync(scope, idempotencyKey, payloadHash, 200, JsonSerializer.Serialize(result), DateTimeOffset.UtcNow.AddDays(30), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        return invoice is null ? null : MapInvoice(invoice);
    }

    public async Task<InvoiceDto?> IssueInvoiceAsync(
        Guid invoiceId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"invoice:issue:{actorId}:{invoiceId}";
        var payloadHash = ComputePayloadHash(new { invoiceId, actorId, action = "issue" });
        var replay = await TryReplayAsync<InvoiceDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var invoice = await dbContext.Invoices.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        if (invoice.Status is not ("Draft" or "ReadyToIssue"))
        {
            throw new DomainException(new("INVOICE_NOT_ISSUABLE", "Fatura mevcut durumunda kesilemez."));
        }

        foreach (var item in invoice.Items)
        {
            var source = await LockDeliveryNoteItemAsync(item.DeliveryNoteItemId, cancellationToken);
            if (source is null || source.RemainingToInvoice < item.QuantityBase)
            {
                throw new DomainException(new("INVOICE_ALLOCATION_CONFLICT", "Fatura allocation irsaliye kalanını aşamaz."));
            }

            source.InvoicedQty += item.QuantityBase;
            source.RemainingToInvoice = source.ShippedQty - source.InvoicedQty - source.WaivedQty;
            dbContext.InvoiceItemAllocations.Add(new InvoiceItemAllocationRecord
            {
                Id = Guid.NewGuid(),
                DeliveryNoteItemId = source.Id,
                InvoiceItemId = item.Id,
                QuantityBase = item.QuantityBase,
                BaseUomId = await ProductBaseUomIdAsync(item.ProductId, cancellationToken),
                PackagingSnapshot = item.PackagingSnapshot,
                PriceSnapshot = JsonSerializer.Serialize(new { item.UnitPrice, invoice.CurrencyCode }),
                TaxSnapshot = item.TaxSnapshot,
                AllocationKind = "Original",
                Status = "Active",
                IdempotencyKey = $"{idempotencyKey}:{item.Id}",
                PayloadHash = payloadHash,
                CreatedAt = DateTimeOffset.UtcNow,
                RowVersion = 1,
            });
        }

        invoice.Status = "Issued";
        invoice.IssuedAt = DateTimeOffset.UtcNow;
        invoice.IssuedBy = actorId;
        var account = await LockOrCreateCurrentAccountAsync(invoice.CustomerId, invoice.CurrencyCode, cancellationToken);
        account.DebitTotal += invoice.GrandTotal;
        account.Balance += invoice.GrandTotal;
        dbContext.CurrentTransactions.Add(new CurrentTransactionRecord
        {
            Id = Guid.NewGuid(),
            CurrentAccountId = account.Id,
            TransactionType = "InvoiceIssued",
            DebitAmount = invoice.GrandTotal,
            CreditAmount = 0,
            CurrencyCode = invoice.CurrencyCode,
            SourceEntityType = nameof(InvoiceRecord),
            SourceEntityId = invoice.Id,
            IdempotencyKey = idempotencyKey,
            CreatedBy = actorId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await auditWriter.AppendAsync(new("InvoiceIssued", nameof(InvoiceRecord), invoice.Id, actorId, correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = MapInvoice(invoice);
        await idempotencyStore.SaveAsync(scope, idempotencyKey, payloadHash, 200, JsonSerializer.Serialize(result), DateTimeOffset.UtcNow.AddDays(30), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<PaymentDto> ApplyPaymentAsync(
        ApplyPaymentRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new DomainException(new("PAYMENT_AMOUNT_INVALID", "Ödeme tutarı sıfırdan büyük olmalıdır."));
        }

        var scope = $"payment:apply:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<PaymentDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (!await dbContext.Customers.AnyAsync(x => x.Id == request.CustomerId && x.Status == "Active" && !x.IsDeleted, cancellationToken))
        {
            throw new DomainException(new("CUSTOMER_NOT_ACTIVE", "Ödeme yalnızca aktif müşteri için uygulanabilir."));
        }

        if (!await dbContext.PaymentMethods.AnyAsync(x => x.Id == request.PaymentMethodId && x.IsActive, cancellationToken))
        {
            throw new DomainException(new("PAYMENT_METHOD_NOT_FOUND", "Aktif ödeme tipi bulunamadı."));
        }

        InvoiceRecord? invoice = null;
        if (request.InvoiceId.HasValue)
        {
            invoice = await dbContext.Invoices.SingleOrDefaultAsync(x => x.Id == request.InvoiceId.Value && x.CustomerId == request.CustomerId, cancellationToken);
            if (invoice is null || invoice.Status is not ("Issued" or "PartiallyPaid"))
            {
                throw new DomainException(new("INVOICE_NOT_PAYABLE", "Ödeme uygulanacak issued fatura bulunamadı."));
            }

            var allocated = await dbContext.PaymentAllocations.Where(x => x.InvoiceId == invoice.Id).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
            if (allocated + request.Amount > invoice.GrandTotal)
            {
                throw new DomainException(new("OVER_PAYMENT", "Ödeme fatura kalanını aşamaz."));
            }
        }

        var account = await LockOrCreateCurrentAccountAsync(request.CustomerId, "TRY", cancellationToken);
        var payment = new PaymentRecord
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            CurrencyCode = "TRY",
            PaymentMethodId = request.PaymentMethodId,
            Status = "Applied",
            Reference = request.Reference,
            AppliedAt = DateTimeOffset.UtcNow,
            RowVersion = 1,
        };
        dbContext.Payments.Add(payment);
        if (invoice is not null)
        {
            dbContext.PaymentAllocations.Add(new PaymentAllocationRecord
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                Amount = request.Amount,
            });
            var allocated = await dbContext.PaymentAllocations.Where(x => x.InvoiceId == invoice.Id).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
            invoice.Status = allocated + request.Amount >= invoice.GrandTotal ? "Paid" : "PartiallyPaid";
        }

        account.CreditTotal += request.Amount;
        account.Balance -= request.Amount;
        dbContext.CurrentTransactions.Add(new CurrentTransactionRecord
        {
            Id = Guid.NewGuid(),
            CurrentAccountId = account.Id,
            TransactionType = "PaymentApplied",
            DebitAmount = 0,
            CreditAmount = request.Amount,
            CurrencyCode = "TRY",
            SourceEntityType = nameof(PaymentRecord),
            SourceEntityId = payment.Id,
            IdempotencyKey = idempotencyKey,
            CreatedBy = actorId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await auditWriter.AppendAsync(new("PaymentApplied", nameof(PaymentRecord), payment.Id, actorId, correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = new PaymentDto(payment.Id, payment.CustomerId, payment.Amount, payment.PaymentMethodId, payment.Status, invoice?.Id, payment.AppliedAt, MapCurrentAccount(account));
        await idempotencyStore.SaveAsync(scope, idempotencyKey, payloadHash, 200, JsonSerializer.Serialize(result), DateTimeOffset.UtcNow.AddDays(30), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<CurrentAccountDto?> GetCurrentAccountAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.CurrentAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
        return account is null ? null : MapCurrentAccount(account);
    }

    private async Task<SalesOrderItemRecord?> LockSalesOrderItemAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.SalesOrderItems.FromSqlInterpolated($"SELECT * FROM sales_order_items WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

    private async Task<DeliveryNoteItemRecord?> LockDeliveryNoteItemAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.DeliveryNoteItems.FromSqlInterpolated($"SELECT * FROM delivery_note_items WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

    private async Task<Guid> ProductBaseUomIdAsync(Guid productId, CancellationToken cancellationToken)
        => await dbContext.Products.Where(x => x.Id == productId).Select(x => x.BaseUomId).SingleAsync(cancellationToken);

    private async Task<CurrentAccountRecord> LockOrCreateCurrentAccountAsync(Guid customerId, string currencyCode, CancellationToken cancellationToken)
    {
        var account = await dbContext.CurrentAccounts.FromSqlInterpolated($"SELECT * FROM current_accounts WHERE customer_id = {customerId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);
        if (account is not null)
        {
            return account;
        }

        account = new CurrentAccountRecord
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CurrencyCode = currencyCode,
            DebitTotal = 0,
            CreditTotal = 0,
            Balance = 0,
            RowVersion = 1,
        };
        dbContext.CurrentAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

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

    private static decimal TaxRateFromSnapshot(string snapshot)
    {
        using var document = JsonDocument.Parse(snapshot);
        return document.RootElement.TryGetProperty("rate", out var rate) ? rate.GetDecimal() : 0m;
    }

    private static DeliveryNoteDto MapDeliveryNote(DeliveryNoteRecord note)
        => new(note.Id, note.DocumentNumber, note.SalesOrderId, note.CustomerId, note.Status, note.IssuedAt, note.Items.Select(x => new DeliveryNoteItemDto(x.Id, x.SalesOrderItemId, x.ProductId, x.QuantityBase, x.EnteredQuantity, x.EnteredPackagingId, x.ShippedQty, x.InvoicedQty, x.WaivedQty, x.RemainingToInvoice, x.PackagingSnapshot, x.RowVersion)).ToArray(), note.RowVersion);

    private static InvoiceDto MapInvoice(InvoiceRecord invoice)
        => new(invoice.Id, invoice.InvoiceNumber, invoice.CustomerId, invoice.Status, invoice.CurrencyCode, invoice.Subtotal, invoice.TaxTotal, invoice.GrandTotal, invoice.Items.Select(x => new InvoiceItemDto(x.Id, x.DeliveryNoteItemId, x.ProductId, x.QuantityBase, x.EnteredQuantity, x.EnteredPackagingId, x.UnitPrice, x.LineTotal, 1)).ToArray(), invoice.IssuedAt, invoice.RowVersion);

    private static CurrentAccountDto MapCurrentAccount(CurrentAccountRecord account)
        => new(account.CustomerId, account.CurrencyCode, account.DebitTotal, account.CreditTotal, account.Balance, account.RowVersion);
}
