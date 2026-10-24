using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Firmeza.Web.Data;

namespace Firmeza.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalProducts = await _context.Products.CountAsync();
        var totalCustomers = await _context.Customers.CountAsync();
        var totalSales = await _context.Sales.CountAsync();

        ViewBag.TotalProducts = totalProducts;
        ViewBag.TotalCustomers = totalCustomers;
        ViewBag.TotalSales = totalSales;

        return View();
    }
    [HttpGet]
    public IActionResult ValidarEdad()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ValidarEdad(string edadTexto)
    {
        try
        {
            int edad = int.Parse(edadTexto);
            
            ViewBag.Mensaje = $"¡Perfecto! Tienes {edad} años.";
            ViewBag.TipoAlerta = "success";
        }
        catch (FormatException)
        {
            ViewBag.Mensaje = "Error: La edad ingresada no es válida. Por favor, ingresa solo números enteros.";
            ViewBag.TipoAlerta = "danger";
        }
        catch (Exception)
        {
            ViewBag.Mensaje = "Ocurrió un error inesperado al procesar la edad.";
            ViewBag.TipoAlerta = "warning";
        }

        return View();
    }
}
