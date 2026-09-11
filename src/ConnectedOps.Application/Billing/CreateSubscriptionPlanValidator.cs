using FluentValidation;

namespace ConnectedOps.Application.Billing;

public sealed class CreateSubscriptionPlanValidator : AbstractValidator<CreateSubscriptionPlanRequest>
{
    public CreateSubscriptionPlanValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Plan name is required.")
            .MaximumLength(100).WithMessage("Plan name cannot exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Plan code is required.")
            .MaximumLength(50).WithMessage("Plan code cannot exceed 50 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency code must be 3 characters.");

        RuleFor(x => x.MaxVehicles)
            .GreaterThan(0).WithMessage("Max vehicles must be at least 1.");

        RuleFor(x => x.MaxAssets)
            .GreaterThan(0).WithMessage("Max assets must be at least 1.");

        RuleFor(x => x.MaxUsers)
            .GreaterThan(0).WithMessage("Max users must be at least 1.");
    }
}
