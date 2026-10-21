namespace Firmeza.Web.Models;

public static class SaleCalculator
{
    public const decimal TaxRate = 0.19m;

    public static (decimal Subtotal, decimal Tax, decimal Total) Calculate(IEnumerable<(decimal Price, int Quantity)> lines)
    {
        var subtotal = Math.Round(lines.Sum(line => line.Price * line.Quantity), 2, MidpointRounding.AwayFromZero);
        var tax = Math.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        return (subtotal, tax, subtotal + tax);
    }
}
