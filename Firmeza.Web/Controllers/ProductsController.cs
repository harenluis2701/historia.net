using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Web.Controllers
{
    public class ProductsController : Controller
    {
        // Creamos una variable para guardar "la llave" de la base de datos
        private readonly ApplicationDbContext _context;

        // 1. Inyectamos la llave en el constructor (cuando el controlador nace, pide la llave)
        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 2. Modificamos el método Index para que sea asíncrono (que espere a la base de datos sin congelar la app)
        public async Task<IActionResult> Index()
        {
            // Vamos a la base de datos, tomamos la tabla Products y la convertimos en una lista de C#
            var products = await _context.Products.ToListAsync();
            
            // Retornamos la vista, pero esta vez le pasamos la lista de productos llena
            return View(products);
        }
        // Este método solo sirve para mostrar la pantalla del formulario vacío
        public IActionResult Create()
        {
            return View();
        }
        // La etiqueta [HttpPost] le dice al sistema: "Este método solo funciona cuando envían un formulario"
        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            // 1. Verificamos si los datos del formulario son válidos (por ejemplo, que no vengan campos vacíos)
            if (ModelState.IsValid)
            {
                // 2. Tomamos el producto que llegó del formulario y lo preparamos para guardarlo
                _context.Products.Add(product);
        
                // 3. Guardamos los cambios permanentemente en nuestra base de datos PostgreSQL
                await _context.SaveChangesAsync();
        
                // 4. Redirigimos al usuario de vuelta a la pantalla principal (Index) para que vea la lista actualizada
                return RedirectToAction(nameof(Index));
            }

            // Si los datos eran inválidos, le devolvemos el formulario con los datos que intentó meter para que corrija
            return View(product);
        }
        // --- HABITACIÓN PARA MOSTRAR EL FORMULARIO DE EDICIÓN ---
// Este método recibe el 'id' del producto que el usuario quiere editar
        public async Task<IActionResult> Edit(int? id)
        {
            // 1. Si no nos envían ningún, id, mostramos una página de error de "No Encontrado"
            if (id == null)
            {
                return NotFound();
            }

            // 2. Vamos a la base de datos y buscamos el producto específico por su id
            var product = await _context.Products.FindAsync(id);

            // 3. Si el producto no existe en la base de datos, mostramos "No Encontrado"
            if (product == null)
            {
                return NotFound();
            }

            // 4. Si todo está bien, le enviamos ese producto a la vista para que llene el formulario
            return View(product);
        }
        // Este método recibe el ID de la URL y los datos del producto desde el formulario
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            // 1. Verificamos que el ID de la URL coincida con el ID del producto oculto en el formulario
            if (id != product.Id)
            {
                return NotFound();
            }

            // 2. Verificamos que los datos del formulario sean válidos
            if (ModelState.IsValid)
            {
                // 3. Le decimos a Entity Framework que este producto fue modificado
                _context.Update(product);
        
                // 4. Guardamos los cambios permanentemente en PostgreSQL
                await _context.SaveChangesAsync();
        
                // 5. Redirigimos al usuario a la lista de productos
                return RedirectToAction(nameof(Index));
            }

            // Si los datos son inválidos, le devolvemos el formulario para que corrija
            return View(product);
        }
        
        // 1. Muestra la pantalla de confirmación al usuario
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            // Busca el producto en la base de datos para mostrarle al usuario qué va a borrar
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

// 2. Recibe la confirmación del formulario y borra el producto permanentemente
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Le dice a Entity Framework que elimine esta fila
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
    
            // Regresa a la lista de productos
            return RedirectToAction(nameof(Index));
        }
    }
}   