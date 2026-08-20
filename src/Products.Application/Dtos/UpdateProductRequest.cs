namespace Products.Application.Dtos;

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price);
