using Microsoft.EntityFrameworkCore;
using Products.Application.Abstractions;
using Products.Domain.Entities;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure.Repositories;

public sealed class ProductRepository(ProductsDbContext dbContext) : IProductRepository
{
    private readonly ProductsDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _dbContext.Products
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.Name, $"%{name}%"))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByStockRangeAsync(int min, int max, CancellationToken cancellationToken = default)
        => await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Stock >= min && p.Stock <= max)
            .OrderBy(p => p.Stock)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.Entry(product).State = EntityState.Detached;
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.Products
            .Where(p => p.Id == product.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Name, product.Name)
                .SetProperty(p => p.Description, product.Description)
                .SetProperty(p => p.Price, product.Price)
                .SetProperty(p => p.Stock, product.Stock)
                .SetProperty(p => p.UpdatedAtUtc, product.UpdatedAtUtc), cancellationToken);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.Products
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return affected > 0;
    }

    public async Task<bool> IncrementStockAsync(int id, int quantity, CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.Products
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Stock, p => p.Stock + quantity), cancellationToken);

        return affected > 0;
    }

    public async Task<bool> DecrementStockAsync(int id, int quantity, CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.Products
            .Where(p => p.Id == id && p.Stock >= quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Stock, p => p.Stock - quantity), cancellationToken);

        return affected > 0;
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => _dbContext.Products.AnyAsync(p => p.Id == id, cancellationToken);
}
