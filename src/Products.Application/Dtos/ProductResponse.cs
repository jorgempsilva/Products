namespace Products.Application.Dtos;

public sealed record ProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
