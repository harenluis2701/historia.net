namespace Firmeza.Api.Services;

public class SmtpEmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Firmeza";
    public bool EnableSsl { get; set; } = true;
}
