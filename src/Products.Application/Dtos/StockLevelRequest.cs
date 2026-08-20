namespace Products.Application.Dtos;

public sealed record StockLevelRequest(int Min = 0, int Max = int.MaxValue) : PaginationRequest;
