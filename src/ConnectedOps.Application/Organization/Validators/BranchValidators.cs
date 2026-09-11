using FluentValidation;

namespace ConnectedOps.Application.Organization.Validators;

public sealed class CreateBranchRequestValidator : AbstractValidator<CreateBranchRequest>
{
    public CreateBranchRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(200).WithMessage("Branch name cannot exceed 200 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Branch code is required.")
            .MaximumLength(50).WithMessage("Branch code cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email is invalid.")
            .MaximumLength(250).WithMessage("Email cannot exceed 250 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");

        RuleFor(x => x.AddressLine1)
            .MaximumLength(250).WithMessage("Address line 1 cannot exceed 250 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(250).WithMessage("Address line 2 cannot exceed 250 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.StateOrProvince)
            .MaximumLength(100).WithMessage("State or province cannot exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");

        RuleFor(x => x.CountryCode)
            .MaximumLength(10).WithMessage("Country code cannot exceed 10 characters.");

        RuleFor(x => x.TimeZoneId)
            .MaximumLength(100).WithMessage("Time zone cannot exceed 100 characters.");
    }
}

public sealed class UpdateBranchRequestValidator : AbstractValidator<UpdateBranchRequest>
{
    public UpdateBranchRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(200).WithMessage("Branch name cannot exceed 200 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Branch code is required.")
            .MaximumLength(50).WithMessage("Branch code cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email is invalid.")
            .MaximumLength(250).WithMessage("Email cannot exceed 250 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");

        RuleFor(x => x.AddressLine1)
            .MaximumLength(250).WithMessage("Address line 1 cannot exceed 250 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(250).WithMessage("Address line 2 cannot exceed 250 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.StateOrProvince)
            .MaximumLength(100).WithMessage("State or province cannot exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");

        RuleFor(x => x.CountryCode)
            .MaximumLength(10).WithMessage("Country code cannot exceed 10 characters.");

        RuleFor(x => x.TimeZoneId)
            .MaximumLength(100).WithMessage("Time zone cannot exceed 100 characters.");
    }
}
