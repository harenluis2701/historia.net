using System.Security.Claims;
using AutoMapper;
using Firmeza.Api.Dtos;
using Firmeza.Api.Security;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController(ApplicationDbContext db, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.FullName.Contains(search) || c.Document.Contains(search) || c.Email.Contains(search));
        var customers = await query.OrderBy(c => c.FullName).ToListAsync(cancellationToken);
        return Ok(mapper.Map<IReadOnlyList<CustomerDto>>(customers));
    }

    [HttpGet("me")]
    [Authorize(Policy = ApiRoles.CustomerPolicy)]
    public async Task<ActionResult<CustomerDto>> GetMe(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower(), cancellationToken);
        return customer is null ? NotFound() : Ok(mapper.Map<CustomerDto>(customer));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return customer is null ? NotFound() : Ok(mapper.Map<CustomerDto>(customer));
    }

    [HttpPost]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDto>> Create(CustomerInputDto input, CancellationToken cancellationToken)
    {
        if (await db.Customers.AnyAsync(c => c.Document == input.Document ||
            (input.Email != "" && c.Email.ToLower() == input.Email.ToLower()), cancellationToken))
            return Conflict(new { message = "Ya existe un cliente con ese documento o correo." });
        var customer = mapper.Map<Customer>(input);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, mapper.Map<CustomerDto>(customer));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    public async Task<ActionResult<CustomerDto>> Update(int id, CustomerInputDto input, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FindAsync([id], cancellationToken);
        if (customer is null) return NotFound();
        if (await db.Customers.AnyAsync(c => c.Id != id && (c.Document == input.Document ||
            (input.Email != "" && c.Email.ToLower() == input.Email.ToLower())), cancellationToken))
            return Conflict(new { message = "Ya existe otro cliente con ese documento o correo." });
        mapper.Map(input, customer);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(mapper.Map<CustomerDto>(customer));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FindAsync([id], cancellationToken);
        if (customer is null) return NotFound();
        if (await db.Sales.AnyAsync(s => s.CustomerId == id, cancellationToken))
            return Conflict(new { message = "No se puede eliminar el cliente porque tiene ventas asociadas." });
        db.Customers.Remove(customer);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
