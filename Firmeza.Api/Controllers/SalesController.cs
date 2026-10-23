using System.Security.Claims;
using AutoMapper;
using Firmeza.Api.Dtos;
using Firmeza.Api.Security;
using Firmeza.Api.Services;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public class SalesController(ApplicationDbContext db, IMapper mapper, IAppEmailSender emailSender, IReceiptPdfService receiptPdf) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetAll(CancellationToken cancellationToken)
    {
        var query = SaleQuery();
        if (User.IsInRole(ApiRoles.Customer))
        {
            var customer = await FindCurrentCustomer(cancellationToken);
            if (customer is null) return NotFound(new { message = "No se encontró el perfil de cliente de esta cuenta." });
            query = query.Where(s => s.CustomerId == customer.Id);
        }
        else if (!User.IsInRole(ApiRoles.Admin)) return Forbid();
        return Ok(mapper.Map<IReadOnlyList<SaleDto>>(await query.OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var sale = await SaleQuery().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sale is null) return NotFound();
        if (User.IsInRole(ApiRoles.Customer))
        {
            var customer = await FindCurrentCustomer(cancellationToken);
            if (customer is null || customer.Id != sale.CustomerId) return Forbid();
        }
        else if (!User.IsInRole(ApiRoles.Admin)) return Forbid();
        return Ok(mapper.Map<SaleDto>(sale));
    }

    [HttpPost]
    [Authorize(Roles = ApiRoles.Admin + "," + ApiRoles.Customer)]
    [ProducesResponseType<SaleDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SaleDto>> Create(SaleInputDto input, CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomer(input.CustomerId, cancellationToken);
        if (customer is null) return Forbid();
        var requested = NormalizeItems(input);
        if (requested is null) return BadRequest(new { message = "No se permiten productos repetidos en el detalle de una venta." });
        var products = await db.Products.Where(p => requested.Keys.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        if (products.Count != requested.Count) return BadRequest(new { message = "Uno o más productos no existen." });
        var shortage = requested.FirstOrDefault(item => products[item.Key].Stock < item.Value);
        if (!shortage.Equals(default(KeyValuePair<int, int>)))
            return Conflict(new { message = $"Inventario insuficiente para '{products[shortage.Key].Name}'. Disponible: {products[shortage.Key].Stock}." });

        var lines = requested.Select(item => (products[item.Key].Price, item.Value)).ToList();
        var totals = SaleCalculator.Calculate(lines);
        var sale = new Sale
        {
            Customer = customer,
            CreatedAt = DateTime.UtcNow,
            Subtotal = totals.Subtotal,
            Tax = totals.Tax,
            Total = totals.Total,
            Items = requested.Select(item => new SaleItem { Product = products[item.Key], Quantity = item.Value, UnitPrice = products[item.Key].Price }).ToList()
        };
        foreach (var item in requested) products[item.Key].Stock -= item.Value;
        db.Sales.Add(sale);
        await db.SaveChangesAsync(cancellationToken);

        var created = await SaleQuery().FirstAsync(s => s.Id == sale.Id, cancellationToken);
        var pdf = receiptPdf.Generate(created);
        var emailSent = await emailSender.SendWithAttachmentAsync(customer.Email, $"Confirmación de compra #{sale.Id}",
            $"Hola {customer.FullName}, adjuntamos el comprobante de tu compra #{sale.Id} por {sale.Total:C}.",
            pdf, $"comprobante-firmeza-{sale.Id}.pdf", cancellationToken);
        Response.Headers["X-Email-Sent"] = emailSent.ToString().ToLowerInvariant();
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, mapper.Map<SaleDto>(created));
    }

    [HttpGet("{id:int}/receipt")]
    [Authorize(Roles = ApiRoles.Admin + "," + ApiRoles.Customer)]
    public async Task<IActionResult> Receipt(int id, CancellationToken cancellationToken)
    {
        var sale = await SaleQuery().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sale is null) return NotFound();

        if (User.IsInRole(ApiRoles.Customer))
        {
            var customer = await FindCurrentCustomer(cancellationToken);
            if (customer is null || customer.Id != sale.CustomerId) return Forbid();
        }
        else if (!User.IsInRole(ApiRoles.Admin)) return Forbid();

        return File(receiptPdf.Generate(sale), "application/pdf", $"comprobante-firmeza-{sale.Id}.pdf");
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    public async Task<ActionResult<SaleDto>> Update(int id, SaleInputDto input, CancellationToken cancellationToken)
    {
        var sale = await db.Sales.Include(s => s.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sale is null) return NotFound();
        var customer = await db.Customers.FindAsync([input.CustomerId], cancellationToken);
        if (customer is null) return BadRequest(new { message = "El cliente no existe." });
        var requested = NormalizeItems(input);
        if (requested is null) return BadRequest(new { message = "No se permiten productos repetidos en el detalle de una venta." });
        var oldQuantities = sale.Items.GroupBy(i => i.ProductId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        var allProductIds = oldQuantities.Keys.Union(requested.Keys).ToArray();
        var products = await db.Products.Where(p => allProductIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        if (products.Count != allProductIds.Length || requested.Keys.Any(id => !products.ContainsKey(id)))
            return BadRequest(new { message = "Uno o más productos no existen." });
        foreach (var item in requested)
        {
            var availableAfterRestore = products[item.Key].Stock + oldQuantities.GetValueOrDefault(item.Key);
            if (availableAfterRestore < item.Value)
                return Conflict(new { message = $"Inventario insuficiente para '{products[item.Key].Name}'. Disponible: {availableAfterRestore}." });
        }

        foreach (var old in oldQuantities) products[old.Key].Stock += old.Value;
        foreach (var item in requested) products[item.Key].Stock -= item.Value;
        db.SaleItems.RemoveRange(sale.Items);
        var totals = SaleCalculator.Calculate(requested.Select(item => (products[item.Key].Price, item.Value)));
        sale.Customer = customer;
        sale.CreatedAt = DateTime.UtcNow;
        sale.Subtotal = totals.Subtotal;
        sale.Tax = totals.Tax;
        sale.Total = totals.Total;
        sale.Items = requested.Select(item => new SaleItem { Product = products[item.Key], Quantity = item.Value, UnitPrice = products[item.Key].Price }).ToList();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(mapper.Map<SaleDto>(sale));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var sale = await db.Sales.Include(s => s.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sale is null) return NotFound();
        foreach (var item in sale.Items) item.Product.Stock += item.Quantity;
        db.Sales.Remove(sale);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<Sale> SaleQuery() => db.Sales.AsNoTracking().Include(s => s.Customer).Include(s => s.Items).ThenInclude(i => i.Product);

    private async Task<Customer?> FindCurrentCustomer(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        return string.IsNullOrWhiteSpace(email) ? null : await db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    private async Task<Customer?> ResolveCustomer(int requestedId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(ApiRoles.Admin)) return await db.Customers.FindAsync([requestedId], cancellationToken);
        var current = await FindCurrentCustomer(cancellationToken);
        return current?.Id == requestedId ? current : null;
    }

    private static Dictionary<int, int>? NormalizeItems(SaleInputDto input)
    {
        if (input.Items.Count == 0 || input.Items.GroupBy(i => i.ProductId).Any(group => group.Count() > 1)) return null;
        return input.Items.ToDictionary(item => item.ProductId, item => item.Quantity);
    }
}
