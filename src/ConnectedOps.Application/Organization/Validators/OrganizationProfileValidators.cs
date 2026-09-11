using FluentValidation;

namespace ConnectedOps.Application.Organization.Validators;

public sealed class UpdateOrganizationProfileRequestValidator
    : AbstractValidator<UpdateOrganizationProfileRequest>
{
    public UpdateOrganizationProfileRequestValidator()
    {
        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Legal name is required.")
            .MaximumLength(250).WithMessage("Legal name cannot exceed 250 characters.");

        RuleFor(x => x.TradeName)
            .MaximumLength(250).WithMessage("Trade name cannot exceed 250 characters.");

        RuleFor(x => x.RegistrationNumber)
            .MaximumLength(100).WithMessage("Registration number cannot exceed 100 characters.");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(100).WithMessage("Tax number cannot exceed 100 characters.");

        RuleFor(x => x.Website)
            .MaximumLength(250).WithMessage("Website cannot exceed 250 characters.");

        RuleFor(x => x.PrimaryContactEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.PrimaryContactEmail))
            .WithMessage("Primary contact email is invalid.")
            .MaximumLength(250).WithMessage("Primary contact email cannot exceed 250 characters.");

        RuleFor(x => x.PrimaryContactPhone)
            .MaximumLength(50).WithMessage("Primary contact phone cannot exceed 50 characters.");

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

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .MaximumLength(10).WithMessage("Currency code cannot exceed 10 characters.");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Time zone is required.")
            .MaximumLength(100).WithMessage("Time zone cannot exceed 100 characters.");
    }
}
