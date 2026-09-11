using FluentValidation;

namespace ConnectedOps.Application.Vehicles.Validators;

public sealed class RecordVehicleOdometerRequestValidator : AbstractValidator<RecordVehicleOdometerRequest>
{
    public RecordVehicleOdometerRequestValidator()
    {
        RuleFor(x => x.Reading)
            .GreaterThanOrEqualTo(0).WithMessage("Odometer reading cannot be negative.");

        RuleFor(x => x.Unit)
            .IsInEnum().WithMessage("Invalid odometer unit.");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Invalid odometer source.");

        RuleFor(x => x.ReadingDateUtc)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Reading date cannot be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}
