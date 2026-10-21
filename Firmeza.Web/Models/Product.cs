using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models;

public class Product
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999999")] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int Stock { get; set; }
}