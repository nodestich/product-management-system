using ProductManagement.Application.Common.DTOs.Product;

namespace ProductManagement.Application.Common.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<ProductDto> DecrementStockAsync(int id, int quantity, CancellationToken ct = default);
    Task<ProductDto> IncrementStockAsync(int id, int quantity, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> SearchAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetByStockLevelAsync(int min, int max, CancellationToken ct = default);


}