using FluentValidation;

namespace ConnectedOps.Application.Maps.Validators;

public sealed class FleetMapTrailQueryParametersValidator : AbstractValidator<FleetMapTrailQueryParameters>
{
    public FleetMapTrailQueryParametersValidator()
    {
        RuleFor(x => x.MaxPoints)
            .InclusiveBetween(10, 5000).WithMessage("Max points must be between 10 and 5000.");

        When(x => x.FromUtc.HasValue && x.ToUtc.HasValue, () =>
        {
            RuleFor(x => x.FromUtc)
                .LessThanOrEqualTo(x => x.ToUtc!.Value).WithMessage("From date cannot be after To date.");

            RuleFor(x => x)
                .Must(x => (x.ToUtc!.Value - x.FromUtc!.Value).TotalDays <= 7)
                .WithMessage("Trail query range cannot exceed 7 days.");
        });
    }
}
