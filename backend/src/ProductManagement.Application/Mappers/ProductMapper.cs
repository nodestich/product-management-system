using System.Linq.Expressions;

using ProductManagement.Application.DTOs.Product;
using ProductManagement.Domain.Models;

namespace ProductManagement.Application.Mappers;

public static class ProductMapper
{

    public static readonly Expression<Func<Product, ProductDto>> ToDtoProjection =
        p => new ProductDto(p.Id, p.Name, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt);

    private static Func<Product, ProductDto> ToDto => field ??= ToDtoProjection.Compile();

    public static ProductDto MapToDto(this Product product) => ToDto(product);

    public static IReadOnlyList<ProductDto> MapToDtos(this IEnumerable<Product> products)
    {
        return [.. products.Select(t => t.MapToDto())];
    }
}