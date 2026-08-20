using Products.Domain.Entities;

namespace Products.Application.Abstractions;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchByNameAsync(string name, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetByStockRangeAsync(int min, int max, int page, int pageSize, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> IncrementStockAsync(int id, int quantity, CancellationToken cancellationToken = default);

    Task<bool> DecrementStockAsync(int id, int quantity, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
