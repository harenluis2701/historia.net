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
[Route("api/products")]
[Authorize]
public class ProductsController(ApplicationDbContext db, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool? inStock,
        CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
        if (inStock == true) query = query.Where(p => p.Stock > 0);
        if (inStock == false) query = query.Where(p => p.Stock == 0);
        var products = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return Ok(mapper.Map<IReadOnlyList<ProductDto>>(products));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return product is null ? NotFound() : Ok(mapper.Map<ProductDto>(product));
    }

    [HttpPost]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDto>> Create(ProductInputDto input, CancellationToken cancellationToken)
    {
        var product = mapper.Map<Product>(input);
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        var result = mapper.Map<ProductDto>(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductInputDto input, CancellationToken cancellationToken)
    {
        var product = await db.Products.FindAsync([id], cancellationToken);
        if (product is null) return NotFound();
        mapper.Map(input, product);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(mapper.Map<ProductDto>(product));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = ApiRoles.AdminPolicy)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await db.Products.FindAsync([id], cancellationToken);
        if (product is null) return NotFound();
        if (await db.SaleItems.AnyAsync(i => i.ProductId == id, cancellationToken))
            return Conflict(new { message = "No se puede eliminar el producto porque pertenece a una venta." });
        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
