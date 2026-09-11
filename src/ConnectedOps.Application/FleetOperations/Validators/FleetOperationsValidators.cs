using FluentValidation;

namespace ConnectedOps.Application.FleetOperations.Validators;

public sealed class CreateFleetShiftRequestValidator : AbstractValidator<CreateFleetShiftRequest>
{
    public CreateFleetShiftRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Shift name is required.")
            .MaximumLength(100).WithMessage("Shift name cannot exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Shift code is required.")
            .MaximumLength(30).WithMessage("Shift code cannot exceed 30 characters.");
    }
}

public sealed class UpdateFleetShiftRequestValidator : AbstractValidator<UpdateFleetShiftRequest>
{
    public UpdateFleetShiftRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Shift name is required.")
            .MaximumLength(100).WithMessage("Shift name cannot exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Shift code is required.")
            .MaximumLength(30).WithMessage("Shift code cannot exceed 30 characters.");
    }
}

public sealed class CreateShiftAssignmentRequestValidator : AbstractValidator<CreateShiftAssignmentRequest>
{
    public CreateShiftAssignmentRequestValidator()
    {
        RuleFor(x => x.FleetShiftId)
            .NotEmpty().WithMessage("FleetShiftId is required.");

        RuleFor(x => x)
            .Must(x => x.DriverId.HasValue || x.VehicleId.HasValue)
            .WithMessage("At least one of DriverId or VehicleId must be specified for shift assignment.");

        RuleFor(x => x.EndDateTimeUtc)
            .GreaterThan(x => x.StartDateTimeUtc)
            .When(x => x.EndDateTimeUtc.HasValue)
            .WithMessage("End date time must be later than start date time.");
    }
}

public sealed class CreateCheckoutRequestValidator : AbstractValidator<CreateCheckoutRequest>
{
    public CreateCheckoutRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("VehicleId is required.");

        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("DriverId is required.");

        RuleFor(x => x.StartOdometer)
            .GreaterThanOrEqualTo(0).WithMessage("Start odometer cannot be negative.");
    }
}

public sealed class CheckInSessionRequestValidator : AbstractValidator<CheckInSessionRequest>
{
    public CheckInSessionRequestValidator()
    {
        RuleFor(x => x.EndOdometer)
            .GreaterThanOrEqualTo(0).WithMessage("End odometer cannot be negative.");
    }
}

public sealed class CreateVehicleHandoverRequestValidator : AbstractValidator<CreateVehicleHandoverRequest>
{
    public CreateVehicleHandoverRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("VehicleId is required.");

        RuleFor(x => x.ToDriverId)
            .NotEmpty().WithMessage("ToDriverId is required.");

        RuleFor(x => x)
            .Must(x => !x.FromDriverId.HasValue || x.FromDriverId.Value != x.ToDriverId)
            .WithMessage("ToDriver cannot be the same as FromDriver.");

        RuleFor(x => x.Odometer)
            .GreaterThanOrEqualTo(0).WithMessage("Odometer reading cannot be negative.");
    }
}

public sealed class CreateOperationalExceptionRequestValidator : AbstractValidator<CreateOperationalExceptionRequest>
{
    public CreateOperationalExceptionRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Exception description is required.")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}

public sealed class ResolveOperationalExceptionRequestValidator : AbstractValidator<ResolveOperationalExceptionRequest>
{
    public ResolveOperationalExceptionRequestValidator()
    {
        RuleFor(x => x.ResolutionNotes)
            .NotEmpty().WithMessage("Resolution notes are required when resolving an exception.")
            .MaximumLength(1000).WithMessage("Resolution notes cannot exceed 1000 characters.");
    }
}
