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

public sealed class CustomerDirectoryService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore,
    ICustomerEmailSender emailSender) : ICustomerDirectoryService
{
    public async Task<CustomerCardDto?> GetCustomerCardAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .Include(x => x.Contacts)
            .Include(x => x.Addresses)
            .SingleOrDefaultAsync(x => x.Id == customerId && !x.IsDeleted, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var membership = CustomerPriceResolver.SelectMembership(
            await dbContext.CustomerPriceGroupMembers
                .AsNoTracking()
                .Where(x => x.CustomerId == customer.Id)
                .Select(x => new PriceGroupMembershipCandidate(x.CustomerPriceGroupId, x.EffectiveFrom, x.EffectiveTo))
                .ToArrayAsync(cancellationToken),
            now);
        CustomerPriceGroupRecord? group = null;
        if (membership is not null)
        {
            group = await dbContext.CustomerPriceGroups
                .AsNoTracking()
                .Include(x => x.PriceList)
                .SingleOrDefaultAsync(x => x.Id == membership.CustomerPriceGroupId, cancellationToken);
        }

        return new CustomerCardDto(
            customer.Id,
            customer.CustomerCode,
            customer.LegalName,
            customer.Status,
            customer.Email,
            customer.Phone,
            customer.TaxNumber,
            customer.TaxOffice,
            customer.CreatedAt,
            group?.Code,
            group?.Name,
            group?.PriceListId,
            group?.PriceList.Code,
            customer.Contacts
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.FullName)
                .Select(x => new CustomerContactDto(x.Id, x.FullName, x.Email, x.Phone, x.RoleTitle, x.IsPrimary, x.IsActive))
                .ToArray(),
            customer.Addresses
                .OrderByDescending(x => x.IsDefault)
                .Select(x => new CustomerAddressDto(
                    x.Id,
                    x.AddressType,
                    x.Title,
                    x.Line1,
                    x.Line2,
                    x.District,
                    x.City,
                    x.PostalCode,
                    x.CountryCode,
                    x.IsDefault,
                    x.IsActive))
                .ToArray());
    }

    public async Task<CustomerContactDto> CreateContactAsync(
        Guid customerId,
        CreateCustomerContactRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new DomainException(new("CONTACT_INVALID", "Yetkili adı zorunludur."));
        }

        var idempotencyScope = $"customer-contact:create:{actorId}:{customerId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<CustomerContactDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var customer = await dbContext.Customers
            .Include(x => x.Contacts)
            .SingleOrDefaultAsync(x => x.Id == customerId && !x.IsDeleted, cancellationToken);
        if (customer is null)
        {
            throw new DomainException(new("CUSTOMER_NOT_FOUND", "Müşteri bulunamadı."));
        }

        if (request.IsPrimary)
        {
            foreach (var existing in customer.Contacts.Where(x => x.IsPrimary))
            {
                existing.IsPrimary = false;
            }
        }

        var contact = new CustomerContactRecord
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            RoleTitle = string.IsNullOrWhiteSpace(request.RoleTitle) ? null : request.RoleTitle.Trim(),
            IsPrimary = request.IsPrimary,
            IsActive = true,
        };
        dbContext.CustomerContacts.Add(contact);
        await auditWriter.AppendAsync(new(
            "CustomerContactCreated",
            nameof(CustomerContactRecord),
            contact.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { contact.FullName, contact.Email, contact.IsPrimary })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = new CustomerContactDto(
            contact.Id,
            contact.FullName,
            contact.Email,
            contact.Phone,
            contact.RoleTitle,
            contact.IsPrimary,
            contact.IsActive);
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

    public async Task<CustomerOutboundEmailDto> SendOutboundEmailAsync(
        Guid customerId,
        SendCustomerEmailRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
        {
            throw new DomainException(new("EMAIL_INVALID", "Konu ve metin zorunludur."));
        }

        var idempotencyScope = $"customer-email:send:{actorId}:{customerId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<CustomerOutboundEmailDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var customer = await dbContext.Customers
            .Include(x => x.Contacts)
            .SingleOrDefaultAsync(x => x.Id == customerId && !x.IsDeleted, cancellationToken);
        if (customer is null)
        {
            throw new DomainException(new("CUSTOMER_NOT_FOUND", "Müşteri bulunamadı."));
        }

        var contact = request.ContactId.HasValue
            ? customer.Contacts.SingleOrDefault(x => x.Id == request.ContactId.Value && x.IsActive)
            : customer.Contacts.FirstOrDefault(x => x.IsPrimary && x.IsActive);
        var to = string.IsNullOrWhiteSpace(request.To) ? contact?.Email ?? customer.Email : request.To.Trim();
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new DomainException(new("EMAIL_RECIPIENT_MISSING", "Kayıtlı e-posta adresi yok."));
        }

        if (!IsStoredRecipient(customer, to))
        {
            throw new DomainException(new(
                "EMAIL_RECIPIENT_NOT_ON_CARD",
                "E-posta yalnızca karttaki veya aktif yetkilideki adrese gönderilir."));
        }

        var message = new CustomerOutboundEmailRecord
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ContactId = contact?.Id,
            ToEmail = to,
            Subject = request.Subject.Trim(),
            Body = request.Body.Trim(),
            Status = "Queued",
            CreatedBy = actorId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        dbContext.CustomerOutboundEmails.Add(message);
        dbContext.OutboxMessages.Add(new OutboxMessageRecord
        {
            Id = Guid.NewGuid(),
            OccurredAt = message.CreatedAt,
            MessageType = "CustomerOutboundEmailQueued",
            Payload = JsonSerializer.Serialize(new { message.Id, message.CustomerId, message.ToEmail }),
        });
        await auditWriter.AppendAsync(new(
            "CustomerOutboundEmailQueued",
            nameof(CustomerOutboundEmailRecord),
            message.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { message.ToEmail, message.Subject, message.Status })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var queued = MapEmail(message);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            201,
            JsonSerializer.Serialize(queued),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dispatch = await emailSender.SendAsync(message.ToEmail, message.Subject, message.Body, cancellationToken);
        if (dispatch.Sent)
        {
            message.Status = "Sent";
            message.SentAt = DateTimeOffset.UtcNow;
            message.LastError = null;
        }
        else
        {
            message.Status = emailSender.IsConfigured ? "Failed" : "Queued";
            message.LastError = dispatch.Error ?? (emailSender.IsConfigured ? "Gönderilemedi." : "SMTP yapılandırılmadı; kuyrukta kaldı.");
        }

        dbContext.CustomerOutboundEmails.Update(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapEmail(message);
    }

    public async Task<IReadOnlyCollection<CustomerOutboundEmailDto>> ListOutboundEmailsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.CustomerOutboundEmails
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToArrayAsync(cancellationToken);
        return rows.Select(MapEmail).ToArray();
    }

    private static bool IsStoredRecipient(CustomerRecord customer, string to)
    {
        if (string.Equals(customer.Email, to, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return customer.Contacts.Any(x =>
            x.IsActive
            && !string.IsNullOrWhiteSpace(x.Email)
            && string.Equals(x.Email, to, StringComparison.OrdinalIgnoreCase));
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
            throw new DomainException(new(
                "IDEMPOTENCY_PAYLOAD_MISMATCH",
                "Aynı Idempotency-Key farklı payload ile tekrar kullanılamaz."));
        }

        return JsonSerializer.Deserialize<T>(stored.ResponseBody);
    }

    private static string ComputePayloadHash(object payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))).ToLowerInvariant();

    private static CustomerOutboundEmailDto MapEmail(CustomerOutboundEmailRecord message)
        => new(
            message.Id,
            message.CustomerId,
            message.ContactId,
            message.ToEmail,
            message.Subject,
            message.Body,
            message.Status,
            message.LastError,
            message.CreatedAt,
            message.SentAt);
}
