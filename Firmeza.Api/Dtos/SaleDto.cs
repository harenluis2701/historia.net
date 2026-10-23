using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Dtos;

public record SaleItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
public record SaleDto(int Id, DateTime CreatedAt, int CustomerId, string CustomerName, decimal Subtotal, decimal Tax, decimal Total, IReadOnlyList<SaleItemDto> Items);

public class SaleInputDto
{
    [Range(1, int.MaxValue)] public int CustomerId { get; set; }
    [Required, MinLength(1)] public List<SaleLineInputDto> Items { get; set; } = [];
}

public class SaleLineInputDto
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}
