using System.Net;
using System.Net.Mail;
using FactoryErp.Application.Sales;
using Microsoft.Extensions.Options;

namespace FactoryErp.Infrastructure.Sales;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
    public bool EnableSsl { get; set; } = true;
}

public sealed class SmtpCustomerEmailSender(IOptions<SmtpOptions> options) : ICustomerEmailSender
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(options.Value.Host) && !string.IsNullOrWhiteSpace(options.Value.From);

    public async Task<EmailDispatchResult> SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new EmailDispatchResult(false, "SMTP yapılandırılmadı; kuyrukta kaldı.");
        }

        try
        {
            using var client = new SmtpClient(options.Value.Host, options.Value.Port)
            {
                EnableSsl = options.Value.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };
            if (!string.IsNullOrWhiteSpace(options.Value.UserName))
            {
                client.Credentials = new NetworkCredential(options.Value.UserName, options.Value.Password);
            }

            using var message = new MailMessage(options.Value.From!, to, subject, body);
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
            return new EmailDispatchResult(true, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new EmailDispatchResult(false, exception.Message);
        }
    }
}
