namespace Products.Domain.Exceptions;

public sealed class ProductIdExhaustedException : DomainException
{
    public ProductIdExhaustedException()
        : base("The product id range (100000-999999) has been exhausted.")
    {
    }
}
