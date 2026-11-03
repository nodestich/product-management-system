using ProductManagement.Application.Common.DTOs.Product;
using ProductManagement.Application.Common.Pagination;

namespace ProductManagement.Application.Common.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<ProductDto> DecrementStockAsync(int id, int quantity, CancellationToken ct = default);
    Task<ProductDto> IncrementStockAsync(int id, int quantity, CancellationToken ct = default);
    Task<PagedResult<ProductDto>> SearchAsync(string? name, int page = 1, int pageSize = PaginationConfig.DefaultPageSize, CancellationToken ct = default);
    Task<PagedResult<ProductDto>> GetByStockLevelAsync(int min, int max, int page = 1, int pageSize = PaginationConfig.DefaultPageSize, CancellationToken ct = default);

}