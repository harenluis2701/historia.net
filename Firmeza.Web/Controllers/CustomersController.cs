using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Firmeza.Web.Data;
using Firmeza.Web.Models;

namespace Firmeza.Web.Controllers;

public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. Mostrar la lista de clientes y manejar el buscador (GET)
    [HttpGet]
    public async Task<IActionResult> Index(string buscar)
    {
        // Preparamos la consulta a la base de datos
        var clientes = from c in _context.Customers select c;

        // Si el usuario escribió algo en el buscador, aplicamos el filtro
        if (!string.IsNullOrEmpty(buscar))
        {
            clientes = clientes.Where(c => c.FullName.Contains(buscar) || c.Document.Contains(buscar));
        }

        // Ejecutamos la consulta y enviamos la lista a la vista
        return View(await clientes.ToListAsync());
    }
}