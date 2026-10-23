namespace Firmeza.Api.Services;

public interface IAppEmailSender
{
    Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
    Task<bool> SendWithAttachmentAsync(string recipient, string subject, string body, byte[] attachment, string fileName, CancellationToken cancellationToken = default);
}
