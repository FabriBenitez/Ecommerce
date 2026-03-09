using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace pruebaPagoMp.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;

    public SmtpEmailService(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("SMTP host no configurado.");
        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("SMTP fromEmail no configurado.");

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_options.User))
            client.Credentials = new NetworkCredential(_options.User, _options.Password);

        await client.SendMailAsync(message);
    }
}
