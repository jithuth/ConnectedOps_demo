using FluentValidation;

namespace ConnectedOps.Application.Drivers.Validators;

public sealed class CreateDriverLicenseRequestValidator : AbstractValidator<CreateDriverLicenseRequest>
{
    public CreateDriverLicenseRequestValidator()
    {
        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("License number is required.")
            .MaximumLength(50).WithMessage("License number cannot exceed 50 characters.");

        RuleFor(x => x.LicenseCountryCode)
            .NotEmpty().WithMessage("License country code is required.")
            .MaximumLength(10).WithMessage("License country code cannot exceed 10 characters.");

        RuleFor(x => x.IssuingAuthority)
            .MaximumLength(150).WithMessage("Issuing authority cannot exceed 150 characters.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.IssueDate!.Value)
            .When(x => x.IssueDate.HasValue && x.ExpiryDate.HasValue)
            .WithMessage("Expiry date cannot be earlier than issue date.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public sealed class UpdateDriverLicenseRequestValidator : AbstractValidator<UpdateDriverLicenseRequest>
{
    public UpdateDriverLicenseRequestValidator()
    {
        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("License number is required.")
            .MaximumLength(50).WithMessage("License number cannot exceed 50 characters.");

        RuleFor(x => x.LicenseCountryCode)
            .NotEmpty().WithMessage("License country code is required.")
            .MaximumLength(10).WithMessage("License country code cannot exceed 10 characters.");

        RuleFor(x => x.IssuingAuthority)
            .MaximumLength(150).WithMessage("Issuing authority cannot exceed 150 characters.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.IssueDate!.Value)
            .When(x => x.IssueDate.HasValue && x.ExpiryDate.HasValue)
            .WithMessage("Expiry date cannot be earlier than issue date.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public sealed class AddDriverLicenseCategoryRequestValidator : AbstractValidator<AddDriverLicenseCategoryRequest>
{
    public AddDriverLicenseCategoryRequestValidator()
    {
        RuleFor(x => x.CategoryCode)
            .NotEmpty().WithMessage("Category code is required.")
            .MaximumLength(20).WithMessage("Category code cannot exceed 20 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Description cannot exceed 250 characters.");

        RuleFor(x => x.ValidTo)
            .GreaterThanOrEqualTo(x => x.ValidFrom!.Value)
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue)
            .WithMessage("ValidTo date cannot be earlier than ValidFrom date.");
    }
}
