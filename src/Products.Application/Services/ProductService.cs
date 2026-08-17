using Products.Application.Abstractions;
using Products.Application.Dtos;
using Products.Domain.Entities;
using Products.Domain.Exceptions;

namespace Products.Application.Services;

public sealed class ProductService(IProductRepository repository, TimeProvider timeProvider) : IProductService
{
    private readonly IProductRepository _repository = repository;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetAllAsync(cancellationToken);
        return products.Select(ToResponse).ToList();
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        return ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _repository.AddAsync(product, cancellationToken);
        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var updated = await _repository.UpdateAsync(product, cancellationToken);
        if (!updated)
        {
            throw new ProductNotFoundException(id);
        }

        return ToResponse(product);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new ProductNotFoundException(id);
        }
    }

    public async Task AddToStockAsync(int id, int quantity, CancellationToken cancellationToken = default)
    {
        EnsurePositiveQuantity(quantity);

        var updated = await _repository.IncrementStockAsync(id, quantity, cancellationToken);
        if (!updated)
        {
            throw new ProductNotFoundException(id);
        }
    }

    public async Task DecrementStockAsync(int id, int quantity, CancellationToken cancellationToken = default)
    {
        EnsurePositiveQuantity(quantity);

        var updated = await _repository.DecrementStockAsync(id, quantity, cancellationToken);
        if (updated)
        {
            return;
        }

        var exists = await _repository.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            throw new ProductNotFoundException(id);
        }

        throw new InsufficientStockException(id, quantity);
    }

    public async Task<IReadOnlyList<ProductResponse>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var products = await _repository.SearchByNameAsync(name.Trim(), cancellationToken);
        return products.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<ProductResponse>> GetByStockRangeAsync(int min, int max, CancellationToken cancellationToken = default)
    {
        if (min < 0 || max < 0 || min > max)
        {
            throw new InvalidStockOperationException(
                $"Invalid stock range: min ({min}) and max ({max}) must be non-negative and min must not exceed max.");
        }

        var products = await _repository.GetByStockRangeAsync(min, max, cancellationToken);
        return products.Select(ToResponse).ToList();
    }

    private static void EnsurePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidStockOperationException($"Quantity must be greater than zero, but was {quantity}.");
        }
    }

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Stock,
        product.CreatedAtUtc,
        product.UpdatedAtUtc);
}
