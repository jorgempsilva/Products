namespace Products.Domain.Exceptions;

public sealed class InvalidStockOperationException(string message) : DomainException(message)
{
}
