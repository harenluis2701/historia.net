using AutoMapper;
using Firmeza.Api.Dtos;
using Firmeza.Api.Mapping;
using Firmeza.Web.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Firmeza.Tests;

public class ApiMappingProfileTests
{
    [Fact]
    public void ProductMapping_MapsEntityToApiDto()
    {
        var mapper = new MapperConfiguration(configuration => configuration.AddProfile<ApiMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var result = mapper.Map<ProductDto>(new Product { Id = 4, Name = "Café", Description = "Molido", Price = 12_500, Stock = 8 });

        Assert.Equal(4, result.Id);
        Assert.Equal("Café", result.Name);
        Assert.Equal(12_500, result.Price);
        Assert.Equal(8, result.Stock);
    }

    [Fact]
    public void SaleMapping_MapsCustomerAndComputedLineTotal()
    {
        var mapper = new MapperConfiguration(configuration => configuration.AddProfile<ApiMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var sale = new Sale
        {
            Id = 9,
            Customer = new Customer { FullName = "Ana Ruiz" },
            Items = [new SaleItem { Product = new Product { Name = "Café" }, Quantity = 2, UnitPrice = 5_000 }]
        };

        var result = mapper.Map<SaleDto>(sale);
        Assert.Equal("Ana Ruiz", result.CustomerName);
        Assert.Equal(10_000, result.Items.Single().LineTotal);
        Assert.Equal("Café", result.Items.Single().ProductName);
    }
}
