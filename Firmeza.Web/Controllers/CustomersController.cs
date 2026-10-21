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
    // 1. Muestra el formulario vacío para crear un cliente (GET)
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // 2. Recibe los datos del formulario y los guarda en la base de datos (POST)
    [HttpPost]
    public async Task<IActionResult> Create(Customer customer)
    {
        if (ModelState.IsValid)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }
    // 3. Muestra el formulario lleno con los datos actuales del cliente (GET)
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        // Busca al cliente en la base de datos usando su ID
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        return View(customer);
    }

    // 4. Recibe los datos modificados y los guarda en la base de datos (POST)
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.Id) return NotFound();

        if (ModelState.IsValid)
        {
            // Le dice a Entity Framework que actualice esta fila con los nuevos datos
            _context.Update(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }
    // 5. Muestra la pantalla de confirmación al usuario (GET)
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        // Busca al cliente en la base de datos para mostrarle al usuario qué va a borrar
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        return View(customer);
    }

    // 6. Recibe la confirmación del formulario y borra el cliente permanentemente (POST)
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            // Le dice a Entity Framework que elimine esta fila
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }
        
        // Regresa a la lista de clientes
        return RedirectToAction(nameof(Index));
    }
}