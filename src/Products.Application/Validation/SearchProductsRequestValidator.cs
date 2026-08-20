using FluentValidation;
using Products.Application.Dtos;

namespace Products.Application.Validation;

public sealed class SearchProductsRequestValidator : AbstractValidator<SearchProductsRequest>
{
    public SearchProductsRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The 'name' query parameter is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        this.ApplyPaginationRules();
    }
}
