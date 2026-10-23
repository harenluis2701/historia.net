using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Firmeza.Api.Services;

public class SmtpEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailSender> logger) : IAppEmailSender
{
    public async Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        var smtp = options.Value;
        if (string.IsNullOrWhiteSpace(smtp.Host) || string.IsNullOrWhiteSpace(smtp.User) ||
            string.IsNullOrWhiteSpace(smtp.Password) || string.IsNullOrWhiteSpace(smtp.FromAddress))
        {
            logger.LogWarning("SMTP no está configurado; se omitió el correo a {Recipient}.", recipient);
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(smtp.FromAddress, smtp.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(recipient));
            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtp.User, smtp.Password)
            };
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or FormatException or InvalidOperationException)
        {
            logger.LogError(ex, "No fue posible enviar el correo a {Recipient} mediante SMTP.", recipient);
            return false;
        }
    }
}
