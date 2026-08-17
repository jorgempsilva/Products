namespace Products.Domain.Exceptions;

public sealed class InsufficientStockException(int productId, int requestedQuantity) : DomainException($"Insufficient stock for product '{productId}': cannot decrement by {requestedQuantity}.")
{
    public int ProductId { get; } = productId;
    public int RequestedQuantity { get; } = requestedQuantity;
}
