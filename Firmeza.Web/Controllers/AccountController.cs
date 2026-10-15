using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Firmeza.Web.Models;

namespace Firmeza.Web.Controllers;

public class AccountController : Controller
{
    // Estas son las herramientas de Identity para manejar usuarios, inicios de sesión y roles
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(
        UserManager<IdentityUser> userManager, 
        SignInManager<IdentityUser> signInManager, 
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    // 1. Muestra la pantalla de registro vacía
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // 2. Recibe los datos del formulario de registro
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Preparamos el nuevo usuario con su correo
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            
            // Identity guarda el usuario y encripta la contraseña por nosotros
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Si el rol (Administrador o Cliente) no existe en la BD, lo creamos
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                // Le asignamos el rol seleccionado al nuevo usuario
                await _userManager.AddToRoleAsync(user, model.Role);

                // Iniciamos sesión automáticamente y lo enviamos a la página principal
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            // Si la contraseña es muy débil, le mostramos el error de Identity
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }
}