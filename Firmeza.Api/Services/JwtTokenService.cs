using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Firmeza.Api.Dtos;
using Firmeza.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Firmeza.Api.Services;

public class JwtTokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    public Task<AuthResponse> CreateAsync(IdentityUser user, IReadOnlyList<string> roles, bool emailSent = false)
    {
        var options = jwtOptions.Value;
        var expires = DateTime.UtcNow.AddMinutes(options.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(options.Issuer, options.Audience, claims, expires: expires, signingCredentials: credentials);
        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(new AuthResponse(encoded, expires, user.Email ?? string.Empty, roles, emailSent));
    }
}
