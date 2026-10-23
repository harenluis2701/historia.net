using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Firmeza.Api.Security;
using Firmeza.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Firmeza.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public async Task CreateAsync_IssuesTokenWithConfiguredRoleAndIssuer()
    {
        var settings = Options.Create(new JwtOptions
        {
            Issuer = "Firmeza.Tests",
            Audience = "Firmeza.Tests.Client",
            Key = new string('k', 48),
            ExpirationMinutes = 60
        });
        var service = new JwtTokenService(settings);
        var response = await service.CreateAsync(new IdentityUser { Id = "user-1", UserName = "client@example.com", Email = "client@example.com" }, [ApiRoles.Customer]);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Equal("Firmeza.Tests", token.Issuer);
        Assert.Contains(token.Audiences, audience => audience == "Firmeza.Tests.Client");
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == ApiRoles.Customer);
        Assert.True(response.ExpiresAtUtc > DateTime.UtcNow);
    }
}
