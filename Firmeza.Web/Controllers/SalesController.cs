using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Firmeza.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Firmeza.Web.Controllers;

public class SalesController(ApplicationDbContext db, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await db.Sales.Include(s => s.Customer).OrderByDescending(s => s.CreatedAt).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new SaleCreateViewModel();
        await LoadOptions(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleCreateViewModel model)
    {
        var customer = await db.Customers.FindAsync(model.CustomerId);
        var product = await db.Products.FindAsync(model.ProductId);
        if (customer is null) ModelState.AddModelError(nameof(model.CustomerId), "Selecciona un cliente válido.");
        if (product is null) ModelState.AddModelError(nameof(model.ProductId), "Selecciona un producto válido.");
        if (product is not null && model.Quantity > product.Stock) ModelState.AddModelError(nameof(model.Quantity), $"Inventario insuficiente. Disponible: {product.Stock}.");
        if (!ModelState.IsValid || customer is null || product is null)
        {
            await LoadOptions(model);
            return View(model);
        }

        var amounts = SaleCalculator.Calculate([(product.Price, model.Quantity)]);
        var sale = new Sale { Customer = customer, CreatedAt = DateTime.UtcNow, Subtotal = amounts.Subtotal, Tax = amounts.Tax, Total = amounts.Total };
        sale.Items.Add(new SaleItem { Product = product, Quantity = model.Quantity, UnitPrice = product.Price });
        product.Stock -= model.Quantity;
        db.Sales.Add(sale);
        await db.SaveChangesAsync();
        await CreateReceipt(sale.Id);
        return RedirectToAction(nameof(Details), new { id = sale.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var sale = await GetSale(id);
        return sale is null ? NotFound() : View(sale);
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        if (await db.Sales.AnyAsync(s => s.Id == id) is false) return NotFound();
        var path = Path.Combine(environment.WebRootPath, "recibos", $"venta-{id}.pdf");
        if (!System.IO.File.Exists(path)) await CreateReceipt(id);
        return PhysicalFile(path, "application/pdf", $"recibo-venta-{id}.pdf");
    }

    private async Task<Sale?> GetSale(int id) => await db.Sales.Include(s => s.Customer).Include(s => s.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(s => s.Id == id);

    private async Task LoadOptions(SaleCreateViewModel model)
    {
        model.Customers = await db.Customers.OrderBy(c => c.FullName).ToListAsync();
        model.Products = await db.Products.Where(p => p.Stock > 0).OrderBy(p => p.Name).ToListAsync();
    }

    private async Task CreateReceipt(int id)
    {
        var sale = await GetSale(id) ?? throw new InvalidOperationException("No se encontró la venta.");
        Directory.CreateDirectory(Path.Combine(environment.WebRootPath, "recibos"));
        var path = Path.Combine(environment.WebRootPath, "recibos", $"venta-{id}.pdf");
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document => document.Page(page =>
        {
            page.Margin(32);
            page.Header().Column(c => { c.Item().Text("FIRMEZA | COMPROBANTE DE VENTA").FontSize(18).Bold().FontColor(Colors.Blue.Darken2); c.Item().Text($"Venta #{sale.Id} · {sale.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}"); });
            page.Content().PaddingVertical(18).Column(column =>
            {
                column.Item().Text($"Cliente: {sale.Customer.FullName}");
                column.Item().Text($"Documento: {sale.Customer.Document} · {sale.Customer.Email}");
                column.Item().PaddingTop(16).Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.RelativeColumn(4); columns.RelativeColumn(); columns.RelativeColumn(2); columns.RelativeColumn(2); });
                    table.Header(header => { header.Cell().Text("Producto").Bold(); header.Cell().Text("Cant.").Bold(); header.Cell().Text("Precio").Bold(); header.Cell().Text("Total").Bold(); });
                    foreach (var line in sale.Items) { table.Cell().Text(line.Product.Name); table.Cell().Text(line.Quantity.ToString()); table.Cell().Text(line.UnitPrice.ToString("C")); table.Cell().Text(line.LineTotal.ToString("C")); }
                });
                column.Item().AlignRight().PaddingTop(14).Text($"Subtotal: {sale.Subtotal:C}");
                column.Item().AlignRight().Text($"IVA (19%): {sale.Tax:C}");
                column.Item().AlignRight().Text($"Total: {sale.Total:C}").FontSize(16).Bold();
            });
            page.Footer().AlignCenter().Text("Gracias por su compra · Firmeza").FontColor(Colors.Grey.Darken1);
        })).GeneratePdf(path);
    }
}
