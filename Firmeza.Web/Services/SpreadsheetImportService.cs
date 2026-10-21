using System.Globalization;
using System.Text;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Firmeza.Web.Services;

public sealed record ImportIssue(string Sheet, int Row, string Message);
public sealed record ImportResult(int Products, int Customers, int Sales, IReadOnlyList<ImportIssue> Issues);

public class SpreadsheetImportService(ApplicationDbContext db)
{
    private static readonly Dictionary<string, string[]> Aliases = new()
    {
        ["genericName"] = ["name", "nombre", "full name", "nombre completo"],
        ["productName"] = ["producto", "product", "product name", "nombre producto"],
        ["customerName"] = ["cliente", "customer", "customer name", "nombre cliente"],
        ["description"] = ["description", "descripcion", "detalle"],
        ["price"] = ["price", "precio", "valor", "unit price", "precio unitario"],
        ["stock"] = ["stock", "existencia", "inventario"],
        ["quantity"] = ["quantity", "cantidad", "qty"],
        ["document"] = ["document", "documento", "identificacion", "cedula", "nit"],
        ["email"] = ["email", "correo", "correo electronico"],
        ["phone"] = ["phone", "telefono", "celular"],
        ["date"] = ["date", "fecha", "fecha venta"],
        ["tax"] = ["tax", "iva"]
    };

    public async Task<ImportResult> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var package = new ExcelPackage(stream);
        var issues = new List<ImportIssue>();
        var products = 0;
        var customers = 0;
        var sales = 0;
        var rowNumber = 0;

        foreach (var sheet in package.Workbook.Worksheets)
        {
            if (sheet.Dimension is null) continue;
            var headers = new Dictionary<string, int>();
            for (var col = 1; col <= sheet.Dimension.End.Column; col++)
            {
                var header = Normalize(sheet.Cells[1, col].Text);
                var field = Aliases.FirstOrDefault(x => x.Value.Contains(header, StringComparer.Ordinal)).Key;
                if (!string.IsNullOrEmpty(field)) headers[field] = col;
            }

            for (var row = 2; row <= sheet.Dimension.End.Row; row++)
            {
                rowNumber++;
                string Get(string key) => headers.TryGetValue(key, out var col) ? sheet.Cells[row, col].Text.Trim() : "";
                var genericName = Get("genericName");
                var productName = Get("productName");
                var customerName = Get("customerName");
                var document = Get("document");
                var priceText = Get("price");
                var quantityText = Get("quantity");
                var hasProduct = productName.Length > 0 || (genericName.Length > 0 && priceText.Length > 0);
                var hasCustomer = document.Length > 0 || customerName.Length > 0 || (genericName.Length > 0 && Get("email").Length > 0);
                var resolvedProductName = productName.Length > 0 ? productName : genericName;
                var rowIssues = new List<string>();
                var touchedProduct = false;
                var touchedCustomer = false;
                var createdSale = false;
                Product? product = null;
                Customer? customer = null;

                if (hasProduct)
                {
                    if (priceText.Length == 0 || !TryDecimal(priceText, out var price) || price <= 0) rowIssues.Add("El precio del producto es obligatorio y debe ser mayor que cero.");
                    else
                    {
                        product = await db.Products.FirstOrDefaultAsync(p => p.Name.ToLower() == resolvedProductName.ToLower(), cancellationToken);
                        if (product is null)
                        {
                            product = new Product { Name = resolvedProductName, Price = price };
                            db.Products.Add(product);
                        }
                        product.Price = price;
                        product.Description = Get("description");
                        if (int.TryParse(Get("stock"), out var stock) && stock >= 0) product.Stock = stock;
                        touchedProduct = true;
                    }
                }

                if (hasCustomer)
                {
                    var resolvedCustomerName = customerName.Length > 0 ? customerName : (hasProduct ? "" : genericName);
                    if (string.IsNullOrWhiteSpace(resolvedCustomerName)) rowIssues.Add("El nombre del cliente es obligatorio.");
                    else if (document.Length == 0) rowIssues.Add("El documento del cliente es obligatorio para identificarlo.");
                    else
                    {
                        customer = await db.Customers.FirstOrDefaultAsync(c => c.Document == document, cancellationToken);
                        if (customer is null)
                        {
                            customer = new Customer { Document = document, FullName = resolvedCustomerName };
                            db.Customers.Add(customer);
                        }
                        customer.FullName = resolvedCustomerName;
                        customer.Email = Get("email");
                        customer.Phone = Get("phone");
                        touchedCustomer = true;
                    }
                }

                var hasQuantity = quantityText.Length > 0;
                if (hasQuantity)
                {
                    if (!int.TryParse(quantityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
                        rowIssues.Add("La cantidad debe ser un entero mayor que cero.");
                    else if (product is null || customer is null)
                        rowIssues.Add("Para importar una venta se requieren producto con precio y cliente con documento en la misma fila.");
                    else if (product.Stock < quantity)
                        rowIssues.Add($"Inventario insuficiente para '{product.Name}'. Disponible: {product.Stock}.");
                    else
                    {
                        var totals = SaleCalculator.Calculate([(product.Price, quantity)]);
                        var sale = new Sale { Customer = customer, CreatedAt = DateTime.TryParse(Get("date"), out var date) ? date.ToUniversalTime() : DateTime.UtcNow, Subtotal = totals.Subtotal, Tax = totals.Tax, Total = totals.Total };
                        sale.Items.Add(new SaleItem { Product = product, Quantity = quantity, UnitPrice = product.Price });
                        product.Stock -= quantity;
                        db.Sales.Add(sale);
                        createdSale = true;
                    }
                }

                if (rowIssues.Count > 0)
                {
                    issues.Add(new ImportIssue(sheet.Name, row, string.Join(" ", rowIssues)));
                    db.ChangeTracker.Clear();
                    continue;
                }
                if (touchedProduct || touchedCustomer || createdSale)
                {
                    await db.SaveChangesAsync(cancellationToken);
                    if (touchedProduct) products++;
                    if (touchedCustomer) customers++;
                    if (createdSale) sales++;
                }
            }
        }
        return new ImportResult(products, customers, sales, issues);
    }

    private static bool TryDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value) ||
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("es-CO"), out value);

    private static string Normalize(string text)
    {
        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return string.Concat(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
    }
}
