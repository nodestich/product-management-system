
namespace ProductManagement.Application.Common.DTOs.Product;

public sealed record ProductDto(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateProductRequest(string Name, decimal Price, int Stock);

public sealed record UpdateProductRequest(string Name, decimal Price);
