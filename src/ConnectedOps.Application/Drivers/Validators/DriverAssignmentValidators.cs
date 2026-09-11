using FluentValidation;

namespace ConnectedOps.Application.Drivers.Validators;

public sealed class CreateDriverVehicleAssignmentRequestValidator : AbstractValidator<CreateDriverVehicleAssignmentRequest>
{
    public CreateDriverVehicleAssignmentRequestValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver is required.");

        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle is required.");

        RuleFor(x => x.AssignmentType)
            .IsInEnum().WithMessage("Invalid assignment type.");

        RuleFor(x => x.AssignedToUtc)
            .GreaterThan(x => x.AssignedFromUtc!.Value)
            .When(x => x.AssignedFromUtc.HasValue && x.AssignedToUtc.HasValue)
            .WithMessage("Assignment end time must be later than start time.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public sealed class EndDriverVehicleAssignmentRequestValidator : AbstractValidator<EndDriverVehicleAssignmentRequest>
{
    public EndDriverVehicleAssignmentRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
