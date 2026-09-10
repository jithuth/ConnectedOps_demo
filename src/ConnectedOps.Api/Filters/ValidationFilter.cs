using ConnectedOps.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConnectedOps.Api.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var argumentType = argument.GetType();

            var validatorType =
                typeof(IValidator<>)
                    .MakeGenericType(argumentType);

            var validator =
                context.HttpContext.RequestServices
                    .GetService(validatorType);

            if (validator is null)
                continue;

            var validationContextType =
                typeof(ValidationContext<>)
                    .MakeGenericType(argumentType);

            var validationContext =
                Activator.CreateInstance(
                    validationContextType,
                    argument);

            if (validationContext is not IValidationContext contextObject)
                continue;

            var validateMethod =
                typeof(IValidator)
                    .GetMethod(
                        nameof(IValidator.ValidateAsync));

            if (validateMethod is null)
                continue;

            var task =
                validateMethod.Invoke(
                    validator,
                    new object?[]
                    {
                        contextObject,
                        context.HttpContext.RequestAborted
                    });

            if (task is not Task<FluentValidation.Results.ValidationResult>
                validationTask)
            {
                continue;
            }

            var validationResult =
                await validationTask;

            if (validationResult.IsValid)
                continue;

            foreach (var failure in validationResult.Errors)
            {
                var propertyName =
                    string.IsNullOrWhiteSpace(failure.PropertyName)
                        ? "request"
                        : failure.PropertyName;

                if (!errors.TryGetValue(
                        propertyName,
                        out var propertyErrors))
                {
                    propertyErrors = new List<string>();

                    errors[propertyName] =
                        propertyErrors;
                }

                if (!propertyErrors.Contains(
                        failure.ErrorMessage,
                        StringComparer.OrdinalIgnoreCase))
                {
                    propertyErrors.Add(
                        failure.ErrorMessage);
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(
                errors.ToDictionary(
                    x => x.Key,
                    x => x.Value.ToArray()));
        }

        await next();
    }
}