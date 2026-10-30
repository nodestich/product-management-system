using Microsoft.EntityFrameworkCore;

using ProductManagement.Domain.Models;

namespace ProductManagement.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}