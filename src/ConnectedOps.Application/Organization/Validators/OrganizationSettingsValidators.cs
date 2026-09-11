using FluentValidation;

namespace ConnectedOps.Application.Organization.Validators;

public sealed class UpdateOrganizationSettingsRequestValidator : AbstractValidator<UpdateOrganizationSettingsRequest>
{
    public UpdateOrganizationSettingsRequestValidator()
    {
        RuleFor(x => x.FiscalYearStartMonth)
            .InclusiveBetween(1, 12).WithMessage("Fiscal year start month must be between 1 and 12.");

        RuleFor(x => x.DefaultWorkingDaysJson)
            .MaximumLength(500).WithMessage("Working days format cannot exceed 500 characters.");
    }
}
