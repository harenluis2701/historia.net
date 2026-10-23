using Firmeza.Api.Dtos;
using Microsoft.AspNetCore.Identity;

namespace Firmeza.Api.Services;

public interface ITokenService
{
    Task<AuthResponse> CreateAsync(IdentityUser user, IReadOnlyList<string> roles, bool emailSent = false);
}
