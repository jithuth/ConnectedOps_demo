using FluentValidation;

namespace ConnectedOps.Application.Telematics.Validators;

public sealed class CreateTrackingDeviceRequestValidator : AbstractValidator<CreateTrackingDeviceRequest>
{
    public CreateTrackingDeviceRequestValidator()
    {
        RuleFor(x => x.DeviceIdentifier)
            .NotEmpty().WithMessage("Device identifier is required.")
            .MaximumLength(100).WithMessage("Device identifier cannot exceed 100 characters.");

        RuleFor(x => x.ProviderId)
            .NotEmpty().WithMessage("Provider is required.");

        RuleFor(x => x.DeviceTypeId)
            .NotEmpty().WithMessage("Device type is required.");

        RuleFor(x => x.IMEI)
            .MaximumLength(30).WithMessage("IMEI cannot exceed 30 characters.")
            .Matches(@"^[0-9]+$").When(x => !string.IsNullOrWhiteSpace(x.IMEI))
            .WithMessage("IMEI must contain only numeric characters.");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).WithMessage("Serial number cannot exceed 100 characters.");

        RuleFor(x => x.Name)
            .MaximumLength(150).WithMessage("Device name cannot exceed 150 characters.");

        RuleFor(x => x.Model)
            .MaximumLength(100).WithMessage("Model cannot exceed 100 characters.");

        RuleFor(x => x.Manufacturer)
            .MaximumLength(100).WithMessage("Manufacturer cannot exceed 100 characters.");

        RuleFor(x => x.FirmwareVersion)
            .MaximumLength(50).WithMessage("Firmware version cannot exceed 50 characters.");

        RuleFor(x => x.SIMNumber)
            .MaximumLength(50).WithMessage("SIM number cannot exceed 50 characters.");

        RuleFor(x => x.SIMICCID)
            .MaximumLength(50).WithMessage("SIM ICCID cannot exceed 50 characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30).WithMessage("Phone number cannot exceed 30 characters.");
    }
}

public sealed class UpdateTrackingDeviceRequestValidator : AbstractValidator<UpdateTrackingDeviceRequest>
{
    public UpdateTrackingDeviceRequestValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty().WithMessage("Provider is required.");

        RuleFor(x => x.DeviceTypeId)
            .NotEmpty().WithMessage("Device type is required.");

        RuleFor(x => x.IMEI)
            .MaximumLength(30).WithMessage("IMEI cannot exceed 30 characters.")
            .Matches(@"^[0-9]+$").When(x => !string.IsNullOrWhiteSpace(x.IMEI))
            .WithMessage("IMEI must contain only numeric characters.");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).WithMessage("Serial number cannot exceed 100 characters.");

        RuleFor(x => x.Name)
            .MaximumLength(150).WithMessage("Device name cannot exceed 150 characters.");

        RuleFor(x => x.Model)
            .MaximumLength(100).WithMessage("Model cannot exceed 100 characters.");

        RuleFor(x => x.Manufacturer)
            .MaximumLength(100).WithMessage("Manufacturer cannot exceed 100 characters.");

        RuleFor(x => x.FirmwareVersion)
            .MaximumLength(50).WithMessage("Firmware version cannot exceed 50 characters.");

        RuleFor(x => x.SIMNumber)
            .MaximumLength(50).WithMessage("SIM number cannot exceed 50 characters.");

        RuleFor(x => x.SIMICCID)
            .MaximumLength(50).WithMessage("SIM ICCID cannot exceed 50 characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30).WithMessage("Phone number cannot exceed 30 characters.");
    }
}

public sealed class CreateTrackingProviderRequestValidator : AbstractValidator<CreateTrackingProviderRequest>
{
    public CreateTrackingProviderRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Provider name is required.")
            .MaximumLength(100).WithMessage("Provider name cannot exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Provider code is required.")
            .MaximumLength(50).WithMessage("Provider code cannot exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9_-]+$").WithMessage("Provider code can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}

public sealed class CreateTrackingDeviceTypeRequestValidator : AbstractValidator<CreateTrackingDeviceTypeRequest>
{
    public CreateTrackingDeviceTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Device type name is required.")
            .MaximumLength(100).WithMessage("Device type name cannot exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Device type code is required.")
            .MaximumLength(50).WithMessage("Device type code cannot exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9_-]+$").WithMessage("Device type code can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}

public sealed class AssignDeviceToVehicleRequestValidator : AbstractValidator<AssignDeviceToVehicleRequest>
{
    public AssignDeviceToVehicleRequestValidator()
    {
        RuleFor(x => x.TrackingDeviceId)
            .NotEmpty().WithMessage("Tracking device is required.");

        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public sealed class CreateDeviceCommandRequestValidator : AbstractValidator<CreateDeviceCommandRequest>
{
    public CreateDeviceCommandRequestValidator()
    {
        RuleFor(x => x.CommandType)
            .IsInEnum().WithMessage("Valid command type is required.");

        RuleFor(x => x.ParametersJson)
            .MaximumLength(2000).WithMessage("Parameters cannot exceed 2000 characters.");
    }
}

public sealed class UpdateTelematicsSettingsRequestValidator : AbstractValidator<UpdateTelematicsSettingsRequest>
{
    public UpdateTelematicsSettingsRequestValidator()
    {
        RuleFor(x => x.OfflineThresholdMinutes)
            .GreaterThanOrEqualTo(1).WithMessage("Offline threshold must be at least 1 minute.")
            .LessThanOrEqualTo(1440).WithMessage("Offline threshold cannot exceed 24 hours (1440 minutes).");

        RuleFor(x => x.TelemetryRetentionDays)
            .GreaterThanOrEqualTo(1).WithMessage("Retention days must be at least 1 day.")
            .LessThanOrEqualTo(3650).WithMessage("Retention days cannot exceed 10 years.");

        RuleFor(x => x.MaxAcceptedFutureMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Max future minutes cannot be negative.")
            .LessThanOrEqualTo(1440).WithMessage("Max future minutes cannot exceed 1440.");

        RuleFor(x => x.MaxAcceptedPastDays)
            .GreaterThanOrEqualTo(1).WithMessage("Max past days must be at least 1 day.")
            .LessThanOrEqualTo(365).WithMessage("Max past days cannot exceed 365 days.");

        RuleFor(x => x.OdometerUpdateThresholdKm)
            .GreaterThanOrEqualTo(0m).WithMessage("Odometer threshold cannot be negative.");

        RuleFor(x => x.OdometerUpdateMinIntervalMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Odometer update interval cannot be negative.");
    }
}
