using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Dtos;

public class RegisterRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(10), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required, StringLength(160)] public string FullName { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Document { get; set; } = string.Empty;
    [StringLength(40)] public string Phone { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
}

public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, string Email, IReadOnlyList<string> Roles, bool EmailSent);
