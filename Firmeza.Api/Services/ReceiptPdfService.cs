using Firmeza.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Firmeza.Api.Services;

public sealed class ReceiptPdfService : IReceiptPdfService
{
    public byte[] Generate(Sale sale)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(document => document.Page(page =>
        {
            page.Margin(36);
            page.Header().Column(header =>
            {
                header.Item().Text("FIRMEZA | COMPROBANTE DE COMPRA").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                header.Item().Text($"Pedido #{sale.Id} · {sale.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}");
            });
            page.Content().PaddingVertical(20).Column(content =>
            {
                content.Item().Text($"Cliente: {sale.Customer.FullName}");
                content.Item().Text($"Documento: {sale.Customer.Document}");
                content.Item().Text($"Correo: {sale.Customer.Email}");
                content.Item().PaddingTop(18).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn();
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Text("Producto").Bold();
                        header.Cell().Text("Cant.").Bold();
                        header.Cell().Text("Precio").Bold();
                        header.Cell().Text("Total").Bold();
                    });
                    foreach (var item in sale.Items)
                    {
                        table.Cell().Text(item.Product.Name);
                        table.Cell().Text(item.Quantity.ToString());
                        table.Cell().Text(item.UnitPrice.ToString("C"));
                        table.Cell().Text((item.UnitPrice * item.Quantity).ToString("C"));
                    }
                });
                content.Item().AlignRight().PaddingTop(15).Text($"Subtotal: {sale.Subtotal:C}");
                content.Item().AlignRight().Text($"IVA (19%): {sale.Tax:C}");
                content.Item().AlignRight().Text($"Total: {sale.Total:C}").FontSize(16).Bold();
            });
            page.Footer().AlignCenter().Text("Gracias por comprar en Firmeza").FontColor(Colors.Grey.Darken1);
        })).GeneratePdf();
    }
}
