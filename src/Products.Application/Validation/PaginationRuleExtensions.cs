using FluentValidation;
using Products.Application.Dtos;

namespace Products.Application.Validation;

public static class PaginationRuleExtensions
{
    public static void ApplyPaginationRules<T>(this AbstractValidator<T> validator)
        where T : PaginationRequest
    {
        validator.RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be greater than or equal to 1.");

        validator.RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationRequest.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationRequest.MaxPageSize}.");
    }
}
