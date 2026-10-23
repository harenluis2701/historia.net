namespace Firmeza.Api.Services;

public interface IAppEmailSender
{
    Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
