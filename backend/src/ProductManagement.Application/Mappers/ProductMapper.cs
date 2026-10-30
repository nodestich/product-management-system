
using ProductManagement.Application.Common.DTOs.Product;
using ProductManagement.Domain.Models;

namespace ProductManagement.Application.Mappers;

public static class ProductMapper
{
    public static ProductDto MapToDto(this Product product)
        => new(
            product.Id,
            product.Name,
            product.Price,
            product.Stock,
            product.CreatedAt,
            product.UpdatedAt);

    public static IReadOnlyList<ProductDto> MapToDtos(this IEnumerable<Product> products)
    {
        return [.. products.Select(t => t.MapToDto())];
    }
}