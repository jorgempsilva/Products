namespace Products.Application.Dtos;

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock);
