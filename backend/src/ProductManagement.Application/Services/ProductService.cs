using Microsoft.Extensions.Logging;

using ProductManagement.Application.Common.DTOs.Product;
using ProductManagement.Application.Common.Interfaces;

namespace ProductManagement.Application.Services;

public class ProductService(IApplicationDbContext context, ILogger<ProductService> logger) : IProductService
{
    private readonly IApplicationDbContext _context = context;
    private readonly ILogger<ProductService> _logger = logger;

    public Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ProductDto> DecrementStockAsync(int id, int quantity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ProductDto>> GetByStockLevelAsync(int min, int max, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ProductDto> IncrementStockAsync(int id, int quantity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ProductDto>> SearchAsync(string name, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}