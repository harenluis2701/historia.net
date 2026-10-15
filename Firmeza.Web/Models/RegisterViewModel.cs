using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Aquí guardaremos si el usuario seleccionó ser Administrador o Cliente
    [Required(ErrorMessage = "Debes seleccionar un rol para este usuario.")]
    public string Role { get; set; } = string.Empty;
}