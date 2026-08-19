using FluentValidation;
using Products.Application.Dtos;

namespace Products.Application.Validation;

public sealed class SearchProductsRequestValidator : AbstractValidator<SearchProductsRequest>
{
    public SearchProductsRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The 'name' query parameter is required.");
    }
}
