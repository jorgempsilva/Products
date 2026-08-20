namespace Products.Application.Dtos;

public sealed record SearchProductsRequest(string? Name) : PaginationRequest;
