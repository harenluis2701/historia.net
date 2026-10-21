using System.Text;
using Firmeza.Web.Services;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace Firmeza.Web.Controllers;

public class ImportsController(SpreadsheetImportService importer) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Index(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) { ModelState.AddModelError("", "Selecciona un archivo .xlsx."); return View(); }
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        { ModelState.AddModelError("", "Solo se admiten archivos .xlsx."); return View(); }
        try
        {
            SpreadsheetLicense.EnsureConfigured();
            await using var stream = file.OpenReadStream();
            return View("Result", await importer.ImportAsync(stream, cancellationToken));
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException)
        {
            ModelState.AddModelError("", $"No se pudo importar el libro: {ex.Message}");
            return View();
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DownloadLog([FromForm] ImportIssue[] issues)
    {
        var csv = new StringBuilder("Hoja;Fila;Inconsistencia\r\n");
        foreach (var issue in issues) csv.Append('"').Append(issue.Sheet.Replace("\"", "\"\"")).Append('"').Append(';').Append(issue.Row).Append(';').Append('"').Append(issue.Message.Replace("\"", "\"\"")).Append('"').Append("\r\n");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "log-importacion.csv");
    }
}
