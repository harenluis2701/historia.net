using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models;

public class SaleCreateViewModel
{
    [Range(1, int.MaxValue)] public int CustomerId { get; set; }
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; } = 1;
    public IReadOnlyList<Customer> Customers { get; set; } = [];
    public IReadOnlyList<Product> Products { get; set; } = [];
}
