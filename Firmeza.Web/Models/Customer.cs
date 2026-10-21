using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models;

public class Customer
{
    public int Id { get; set; }
    [Required] public string Document { get; set; } = string.Empty;
    [Required] public string FullName { get; set; } = string.Empty;
    [EmailAddress] public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}