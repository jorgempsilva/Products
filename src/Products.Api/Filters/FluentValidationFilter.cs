using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Products.Api.Filters;

public sealed class FluentValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
                continue;

            var validationResult = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
                {
                    Title = "Validation failed"
                });
                return;
            }
        }

        await next();
    }
}
