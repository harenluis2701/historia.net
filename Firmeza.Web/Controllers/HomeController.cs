using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Firmeza.Web.Data;

namespace Firmeza.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    // Conectamos el controlador con la base de datos
    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Contamos los registros directamente desde la base de datos
        var totalProducts = await _context.Products.CountAsync();
        var totalCustomers = await _context.Customers.CountAsync();
        var totalSales = await _context.Sales.CountAsync();

        // Enviamos los números a la vista HTML
        ViewBag.TotalProducts = totalProducts;
        ViewBag.TotalCustomers = totalCustomers;
        ViewBag.TotalSales = totalSales;

        return View();
    }
    // Muestra la pantalla vacía para ingresar la edad (GET)
    [HttpGet]
    public IActionResult ValidarEdad()
    {
        return View();
    }

    // Procesa el texto que envía el usuario (POST) y aplica el try-catch
    [HttpPost]
    public IActionResult ValidarEdad(string edadTexto)
    {
        try
        {
            // Intentamos convertir el texto a número entero
            int edad = int.Parse(edadTexto);
            
            // Si funciona, preparamos un mensaje de éxito
            ViewBag.Mensaje = $"¡Perfecto! Tienes {edad} años.";
            ViewBag.TipoAlerta = "success";
        }
        catch (FormatException)
        {
            // Si el usuario escribió letras o símbolos, capturamos el error
            ViewBag.Mensaje = "Error: La edad ingresada no es válida. Por favor, ingresa solo números enteros.";
            ViewBag.TipoAlerta = "danger";
        }
        catch (Exception)
        {
            // Capturamos cualquier otro error inesperado (ej. números demasiado grandes)
            ViewBag.Mensaje = "Ocurrió un error inesperado al procesar la edad.";
            ViewBag.TipoAlerta = "warning";
        }

        return View();
    }
}
