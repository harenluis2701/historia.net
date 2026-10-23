using System.Data;
using Firmeza.Api.Dtos;
using Firmeza.Api.Services;
using Firmeza.Api.Security;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<IdentityUser> users,
    SignInManager<IdentityUser> signIn,
    ApplicationDbContext db,
    ITokenService tokens,
    IAppEmailSender emailSender) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(
            c => c.Document == request.Document || c.Email.ToLower() == request.Email.ToLower(), cancellationToken);
        if (customer is not null &&
            (!string.Equals(customer.Document, request.Document, StringComparison.Ordinal) ||
             !string.Equals(customer.Email, request.Email, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { message = "El documento o correo ya está asociado a otro perfil de cliente." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var user = new IdentityUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true };
        var createUser = await users.CreateAsync(user, request.Password);
        if (!createUser.Succeeded)
            return BadRequest(new { errors = createUser.Errors.Select(e => e.Description) });

        var addRole = await users.AddToRoleAsync(user, ApiRoles.Customer);
        if (!addRole.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo asignar el rol de cliente." });
        }

        if (customer is null) db.Customers.Add(new Customer
        {
            Document = request.Document.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim()
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var emailSent = await emailSender.SendAsync(
            request.Email,
            "Bienvenido a Firmeza",
            $"Hola {request.FullName}, tu cuenta de cliente fue creada correctamente.",
            cancellationToken);
        var roleNames = await users.GetRolesAsync(user);
        var response = await tokens.CreateAsync(user, roleNames.ToArray(), emailSent);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await signIn.CanSignInAsync(user)) return Unauthorized();
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized();
        var roles = await users.GetRolesAsync(user);
        return Ok(await tokens.CreateAsync(user, roles.ToArray()));
    }
}
