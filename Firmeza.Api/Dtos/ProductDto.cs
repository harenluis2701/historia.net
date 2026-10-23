using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Dtos;

public record ProductDto(int Id, string Name, string Description, decimal Price, int Stock);

public class ProductInputDto
{
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [StringLength(1000)] public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true)] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int Stock { get; set; }
}
