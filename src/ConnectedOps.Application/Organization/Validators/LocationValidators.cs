using FluentValidation;

namespace ConnectedOps.Application.Organization.Validators;

public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("Branch ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Location name is required.")
            .MaximumLength(200).WithMessage("Location name cannot exceed 200 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Location code is required.")
            .MaximumLength(50).WithMessage("Location code cannot exceed 50 characters.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.GeofenceRadiusMeters)
            .GreaterThan(0).When(x => x.GeofenceRadiusMeters.HasValue)
            .WithMessage("Geofence radius must be positive.");

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
    }
}

public sealed class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("Branch ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Location name is required.")
            .MaximumLength(200).WithMessage("Location name cannot exceed 200 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Location code is required.")
            .MaximumLength(50).WithMessage("Location code cannot exceed 50 characters.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.GeofenceRadiusMeters)
            .GreaterThan(0).When(x => x.GeofenceRadiusMeters.HasValue)
            .WithMessage("Geofence radius must be positive.");

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
    }
}
