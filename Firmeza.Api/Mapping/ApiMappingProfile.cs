using AutoMapper;
using Firmeza.Api.Dtos;
using Firmeza.Web.Models;

namespace Firmeza.Api.Mapping;

public sealed class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<ProductInputDto, Product>();
        CreateMap<Customer, CustomerDto>();
        CreateMap<CustomerInputDto, Customer>();
        CreateMap<SaleItem, SaleItemDto>()
            .ForCtorParam(nameof(SaleItemDto.ProductName), option => option.MapFrom(source => source.Product.Name))
            .ForCtorParam(nameof(SaleItemDto.LineTotal), option => option.MapFrom(source => source.Quantity * source.UnitPrice));
        CreateMap<Sale, SaleDto>()
            .ForCtorParam(nameof(SaleDto.CustomerName), option => option.MapFrom(source => source.Customer.FullName))
            .ForCtorParam(nameof(SaleDto.Items), option => option.MapFrom(source => source.Items));
    }
}
