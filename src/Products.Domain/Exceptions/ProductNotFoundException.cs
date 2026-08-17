namespace Products.Domain.Exceptions;

public sealed class ProductNotFoundException(int productId) : DomainException($"Product with id '{productId}' was not found.")
{
    public int ProductId { get; } = productId;
}
