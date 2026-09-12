using FluentValidation;

namespace ConnectedOps.Application.Compliance.Validators;

public sealed class CreateComplianceRequirementRequestValidator : AbstractValidator<CreateComplianceRequirementRequest>
{
    public CreateComplianceRequirementRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Requirement code is required.")
            .MaximumLength(50).WithMessage("Requirement code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Requirement name is required.")
            .MaximumLength(150).WithMessage("Requirement name cannot exceed 150 characters.");

        RuleFor(x => x.DefaultReminderDays)
            .GreaterThanOrEqualTo(0).When(x => x.DefaultReminderDays.HasValue)
            .WithMessage("Reminder days must be 0 or greater.");
    }
}

public sealed class UpdateComplianceRequirementRequestValidator : AbstractValidator<UpdateComplianceRequirementRequest>
{
    public UpdateComplianceRequirementRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Requirement name is required.")
            .MaximumLength(150).WithMessage("Requirement name cannot exceed 150 characters.");

        RuleFor(x => x.DefaultReminderDays)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Reminder days must be 0 or greater.");
    }
}

public sealed class CreateComplianceRecordRequestValidator : AbstractValidator<CreateComplianceRecordRequest>
{
    public CreateComplianceRecordRequestValidator()
    {
        RuleFor(x => x.ComplianceRequirementId)
            .NotEmpty().WithMessage("ComplianceRequirementId is required.");

        RuleFor(x => x)
            .Must(x => (x.VehicleId.HasValue ? 1 : 0) + (x.DriverId.HasValue ? 1 : 0) + (x.AssetId.HasValue ? 1 : 0) == 1)
            .WithMessage("Exactly one of VehicleId, DriverId, or AssetId must be provided.");

        RuleFor(x => x)
            .Must(x => !x.IssueDateUtc.HasValue || !x.ExpiryDateUtc.HasValue || x.ExpiryDateUtc >= x.IssueDateUtc)
            .WithMessage("Expiry date cannot be earlier than issue date.");
    }
}

public sealed class CreateComplianceExceptionRequestValidator : AbstractValidator<CreateComplianceExceptionRequest>
{
    public CreateComplianceExceptionRequestValidator()
    {
        RuleFor(x => x.ComplianceRequirementId)
            .NotEmpty().WithMessage("ComplianceRequirementId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Exemption reason is required.")
            .MaximumLength(500).WithMessage("Exemption reason cannot exceed 500 characters.");

        RuleFor(x => x)
            .Must(x => (x.VehicleId.HasValue ? 1 : 0) + (x.DriverId.HasValue ? 1 : 0) + (x.AssetId.HasValue ? 1 : 0) == 1)
            .WithMessage("Exactly one of VehicleId, DriverId, or AssetId must be provided.");

        RuleFor(x => x.EffectiveToUtc)
            .GreaterThanOrEqualTo(x => x.EffectiveFromUtc)
            .WithMessage("Effective to date cannot be earlier than effective from date.");
    }
}
