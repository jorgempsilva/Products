using Products.Application.Dtos;

namespace Products.Application.Abstractions;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task AddToStockAsync(int id, int quantity, CancellationToken cancellationToken = default);

    Task DecrementStockAsync(int id, int quantity, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductResponse>> SearchByNameAsync(string name, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductResponse>> GetByStockRangeAsync(int min, int max, int page, int pageSize, CancellationToken cancellationToken = default);
}
