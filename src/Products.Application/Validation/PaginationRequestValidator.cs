using FluentValidation;
using Products.Application.Dtos;

namespace Products.Application.Validation;

public sealed class PaginationRequestValidator : AbstractValidator<PaginationRequest>
{
    public PaginationRequestValidator()
    {
        this.ApplyPaginationRules();
    }
}
