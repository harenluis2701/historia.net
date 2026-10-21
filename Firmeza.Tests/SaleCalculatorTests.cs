using Firmeza.Web.Models;

namespace Firmeza.Tests;

public class SaleCalculatorTests
{
    [Theory]
    [InlineData(100, 2, 200, 38, 238)]
    [InlineData(19.99, 3, 59.97, 11.39, 71.36)]
    public void Calculate_AddsNineteenPercentTax(decimal price, int quantity, decimal subtotal, decimal tax, decimal total)
    {
        var result = SaleCalculator.Calculate([(price, quantity)]);
        Assert.Equal(subtotal, result.Subtotal);
        Assert.Equal(tax, result.Tax);
        Assert.Equal(total, result.Total);
    }
}
