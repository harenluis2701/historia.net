using Firmeza.Web.Data;
using Firmeza.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Firmeza.Web.Controllers;

public class ExportsController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Excel(string tipo)
    {
        SpreadsheetLicense.EnsureConfigured();
        var (headers, rows) = await GetRows(tipo);
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add(tipo);
        for (var c = 0; c < headers.Length; c++) sheet.Cells[1, c + 1].Value = headers[c];
        for (var r = 0; r < rows.Length; r++) for (var c = 0; c < rows[r].Length; c++) sheet.Cells[r + 2, c + 1].Value = rows[r][c];
        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{tipo}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> Pdf(string tipo)
    {
        var (headers, rows) = await GetRows(tipo);
        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(document => document.Page(page =>
        {
            page.Margin(28);
            page.Header().Text($"Firmeza · Exportación de {tipo}").FontSize(18).Bold();
            page.Content().PaddingVertical(16).Table(table =>
            {
                table.ColumnsDefinition(columns => { foreach (var _ in headers) columns.RelativeColumn(); });
                table.Header(header => { foreach (var item in headers) header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(item).Bold(); });
                foreach (var row in rows) foreach (var cell in row) table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell ?? "");
            });
            page.Footer().AlignRight().Text(DateTime.Now.ToString("g"));
        })).GeneratePdf();
        return File(bytes, "application/pdf", $"{tipo}.pdf");
    }

    private async Task<(string[] Headers, string[][] Rows)> GetRows(string tipo) => tipo.ToLowerInvariant() switch
    {
        "productos" => (["Nombre", "Descripción", "Precio", "Inventario"], (await db.Products.OrderBy(p => p.Name).ToListAsync()).Select(p => new[] { p.Name, p.Description, p.Price.ToString("0.00"), p.Stock.ToString() }).ToArray()),
        "clientes" => (["Documento", "Nombre", "Correo", "Teléfono"], (await db.Customers.OrderBy(c => c.FullName).ToListAsync()).Select(c => new[] { c.Document, c.FullName, c.Email, c.Phone }).ToArray()),
        "ventas" => (["Número", "Fecha", "Cliente", "Subtotal", "IVA", "Total"], (await db.Sales.Include(s => s.Customer).OrderByDescending(s => s.CreatedAt).ToListAsync()).Select(s => new[] { s.Id.ToString(), s.CreatedAt.ToLocalTime().ToString("g"), s.Customer.FullName, s.Subtotal.ToString("0.00"), s.Tax.ToString("0.00"), s.Total.ToString("0.00") }).ToArray()),
        _ => throw new ArgumentException("Tipo de exportación no válido.")
    };
}
