using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ProductManagement.Application.Common.DTOs.Product;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.Common.Logging;
using ProductManagement.Application.Mappers;
using ProductManagement.Domain.Models;

namespace ProductManagement.Application.Services;

public class ProductService(IApplicationDbContext context, ILogger<ProductService> logger) : IProductService
{
    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = Product.Create(request.Name, request.Price, request.Stock);

        context.Products.Add(product);
        await context.SaveChangesAsync(ct);
        ProductServiceLogs.ProductCreated(logger, product.Id);

        return product.MapToDto();
    }


    public async Task<ProductDto> DecrementStockAsync(int id, uint quantity, CancellationToken ct = default)
    {
        if (quantity == 0)
        {
            throw new InvalidStockMutationException(id, quantity);
        }

        var dateNow = DateTime.UtcNow;

        // In a multiple instance scenario we need to make sure that stock decrements are atomic.

        // Returns affected rows count.
        // If 0, either the product doesn't exist or the stock is insufficient.
        var affected = await context.Products
            .Where(p => p.Id == id && p.Stock >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Stock, p => p.Stock - quantity)
                .SetProperty(p => p.UpdatedAt, dateNow), ct);

        if (affected == 0)
        {
            // Get the right exception to throw
            var current = await context.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync(ct);

            throw current is null
                ? new ProductNotFoundException(id)
                : new InsufficientStockException(id, quantity);
        }

        var updated = await context.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == id, ct);

        return updated.MapToDto();
    }

    public async Task<ProductDto> IncrementStockAsync(int id, uint quantity, CancellationToken ct = default)
    {
        if (quantity == 0)
        {
            throw new InvalidStockMutationException(id, quantity);
        }

        var dateNow = DateTime.UtcNow;

        var affected = await context.Products
            .Where(p => p.Id == id && quantity + p.Stock <= Product.MaxStock)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Stock, p => p.Stock + quantity)
                .SetProperty(p => p.UpdatedAt, dateNow), ct);

        if (affected == 0)
        {
            var current =
                await context.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync(ct);

            throw current is null
                ? new ProductNotFoundException(id)
                : new StockOverflowException(id, quantity);
        }

        var updated = await context.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == id, ct);

        return updated.MapToDto();

    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var affectedRows = await context.Products
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(ct);

        if (affectedRows > 0) ProductServiceLogs.ProductDeleted(logger, id);

        return affectedRows > 0;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await context.Products.AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);

        return products.MapToDtos();
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
        {
            ProductServiceLogs.ProductNotFound(logger, id);
            throw new ProductNotFoundException(id);
        }

        return product.MapToDto();
    }

    public async Task<IReadOnlyList<ProductDto>> GetByStockLevelAsync(int min, int max, CancellationToken ct = default)
    {
        if (min > max)
        {
            throw new InvalidStockRangeException(min, max);
        }

        var productsInStockRange = await context.Products
            .AsNoTracking()
            .Where(p => p.Stock >= min && p.Stock <= max)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);

        return productsInStockRange.MapToDtos();
    }

    [SuppressMessage(
     "Globalization",
     "CA1304:Specify CultureInfo",
     Justification = "SQL lower() uses the database collation; .NET culture does not apply to LIKE.")]
    [SuppressMessage(
     "Globalization",
     "CA1311:Specify a culture or use an invariant version",
     Justification = "EF translates only the parameterless ToLower(); ToLower(CultureInfo) and ToLowerInvariant() are not translatable.")]
    public async Task<IReadOnlyList<ProductDto>> SearchAsync(string name, CancellationToken ct = default)
    {
        var formattedName = name.Trim().ToLowerInvariant();

        var products = await context.Products
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.Name.ToLower(), $"%{formattedName}%"))
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);

        return products.MapToDtos();
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await context.Products
          .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
        {
            ProductServiceLogs.ProductNotFound(logger, id);
            throw new ProductNotFoundException(id);
        }

        product.Update(request.Name, request.Price);

        await context.SaveChangesAsync(ct);
        ProductServiceLogs.ProductUpdated(logger, product.Id);

        return product.MapToDto();
    }
}