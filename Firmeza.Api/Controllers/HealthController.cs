using Firmeza.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(ApplicationDbContext db, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Check(CancellationToken cancellationToken)
    {
        try
        {
            var counts = new
            {
                products = await db.Products.CountAsync(cancellationToken),
                customers = await db.Customers.CountAsync(cancellationToken),
                sales = await db.Sales.CountAsync(cancellationToken),
                saleItems = await db.SaleItems.CountAsync(cancellationToken)
            };
            return Ok(new { status = "healthy", database = "connected", tables = counts });
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
        {
            logger.LogError(ex, "No fue posible consultar las tablas de Firmeza.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy", database = "unavailable" });
        }
    }
}
