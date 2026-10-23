namespace Firmeza.Api.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "Firmeza.Api";
    public string Audience { get; set; } = "Firmeza.Clients";
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
