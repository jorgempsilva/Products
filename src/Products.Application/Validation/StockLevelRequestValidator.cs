using FluentValidation;
using Products.Application.Dtos;

namespace Products.Application.Validation;

public sealed class StockLevelRequestValidator : AbstractValidator<StockLevelRequest>
{
    public StockLevelRequestValidator()
    {
        this.ApplyPaginationRules();
    }
}
