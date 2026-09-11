using FluentValidation;

namespace ConnectedOps.Application.Vehicles.Validators;

public sealed class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.VehicleNumber)
            .NotEmpty().WithMessage("Vehicle number is required.")
            .MaximumLength(50).WithMessage("Vehicle number cannot exceed 50 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.MakeId)
            .NotEmpty().WithMessage("Make is required.");

        RuleFor(x => x.ModelId)
            .NotEmpty().WithMessage("Model is required.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(150).WithMessage("Display name cannot exceed 150 characters.");

        RuleFor(x => x.InternalCode)
            .MaximumLength(50).WithMessage("Internal code cannot exceed 50 characters.");

        RuleFor(x => x.RegistrationNumber)
            .MaximumLength(50).WithMessage("Registration number cannot exceed 50 characters.");

        RuleFor(x => x.VIN)
            .MaximumLength(50).WithMessage("VIN cannot exceed 50 characters.");

        RuleFor(x => x.ChassisNumber)
            .MaximumLength(50).WithMessage("Chassis number cannot exceed 50 characters.");

        RuleFor(x => x.EngineNumber)
            .MaximumLength(50).WithMessage("Engine number cannot exceed 50 characters.");

        RuleFor(x => x.ModelYear)
            .InclusiveBetween(1900, 2100).When(x => x.ModelYear.HasValue)
            .WithMessage("Model year must be between 1900 and 2100.");

        RuleFor(x => x.ManufactureYear)
            .InclusiveBetween(1900, 2100).When(x => x.ManufactureYear.HasValue)
            .WithMessage("Manufacture year must be between 1900 and 2100.");

        RuleFor(x => x.InitialOdometer)
            .GreaterThanOrEqualTo(0).WithMessage("Initial odometer cannot be negative.");

        RuleFor(x => x.Color)
            .MaximumLength(50).WithMessage("Color cannot exceed 50 characters.");

        RuleFor(x => x.NumberOfSeats)
            .GreaterThan(0).When(x => x.NumberOfSeats.HasValue)
            .WithMessage("Number of seats must be greater than 0.");

        RuleFor(x => x.GrossVehicleWeight)
            .GreaterThan(0).When(x => x.GrossVehicleWeight.HasValue)
            .WithMessage("Gross vehicle weight must be greater than 0.");

        RuleFor(x => x.PayloadCapacity)
            .GreaterThan(0).When(x => x.PayloadCapacity.HasValue)
            .WithMessage("Payload capacity must be greater than 0.");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).When(x => x.PurchasePrice.HasValue)
            .WithMessage("Purchase price cannot be negative.");

        RuleFor(x => x.MonthlyLeaseCost)
            .GreaterThanOrEqualTo(0).When(x => x.MonthlyLeaseCost.HasValue)
            .WithMessage("Monthly lease cost cannot be negative.");

        RuleFor(x => x)
            .Must(x => !x.LeaseStartDate.HasValue || !x.LeaseEndDate.HasValue || x.LeaseEndDate >= x.LeaseStartDate)
            .WithMessage("Lease end date cannot be earlier than lease start date.");
    }
}

public sealed class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.VehicleNumber)
            .NotEmpty().WithMessage("Vehicle number is required.")
            .MaximumLength(50).WithMessage("Vehicle number cannot exceed 50 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.MakeId)
            .NotEmpty().WithMessage("Make is required.");

        RuleFor(x => x.ModelId)
            .NotEmpty().WithMessage("Model is required.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(150).WithMessage("Display name cannot exceed 150 characters.");

        RuleFor(x => x.InternalCode)
            .MaximumLength(50).WithMessage("Internal code cannot exceed 50 characters.");

        RuleFor(x => x.RegistrationNumber)
            .MaximumLength(50).WithMessage("Registration number cannot exceed 50 characters.");

        RuleFor(x => x.VIN)
            .MaximumLength(50).WithMessage("VIN cannot exceed 50 characters.");

        RuleFor(x => x.ChassisNumber)
            .MaximumLength(50).WithMessage("Chassis number cannot exceed 50 characters.");

        RuleFor(x => x.EngineNumber)
            .MaximumLength(50).WithMessage("Engine number cannot exceed 50 characters.");

        RuleFor(x => x.ModelYear)
            .InclusiveBetween(1900, 2100).When(x => x.ModelYear.HasValue)
            .WithMessage("Model year must be between 1900 and 2100.");

        RuleFor(x => x.ManufactureYear)
            .InclusiveBetween(1900, 2100).When(x => x.ManufactureYear.HasValue)
            .WithMessage("Manufacture year must be between 1900 and 2100.");

        RuleFor(x => x.Color)
            .MaximumLength(50).WithMessage("Color cannot exceed 50 characters.");

        RuleFor(x => x.NumberOfSeats)
            .GreaterThan(0).When(x => x.NumberOfSeats.HasValue)
            .WithMessage("Number of seats must be greater than 0.");

        RuleFor(x => x.GrossVehicleWeight)
            .GreaterThan(0).When(x => x.GrossVehicleWeight.HasValue)
            .WithMessage("Gross vehicle weight must be greater than 0.");

        RuleFor(x => x.PayloadCapacity)
            .GreaterThan(0).When(x => x.PayloadCapacity.HasValue)
            .WithMessage("Payload capacity must be greater than 0.");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).When(x => x.PurchasePrice.HasValue)
            .WithMessage("Purchase price cannot be negative.");

        RuleFor(x => x.MonthlyLeaseCost)
            .GreaterThanOrEqualTo(0).When(x => x.MonthlyLeaseCost.HasValue)
            .WithMessage("Monthly lease cost cannot be negative.");

        RuleFor(x => x)
            .Must(x => !x.LeaseStartDate.HasValue || !x.LeaseEndDate.HasValue || x.LeaseEndDate >= x.LeaseStartDate)
            .WithMessage("Lease end date cannot be earlier than lease start date.");
    }
}

public sealed class ChangeVehicleStatusRequestValidator : AbstractValidator<ChangeVehicleStatusRequest>
{
    public ChangeVehicleStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid vehicle status.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}

public sealed class UpsertVehicleSpecificationRequestValidator : AbstractValidator<UpsertVehicleSpecificationRequest>
{
    public UpsertVehicleSpecificationRequestValidator()
    {
        RuleFor(x => x.EngineCapacityCc)
            .GreaterThan(0).When(x => x.EngineCapacityCc.HasValue)
            .WithMessage("Engine capacity must be greater than 0.");

        RuleFor(x => x.EnginePowerKw)
            .GreaterThan(0).When(x => x.EnginePowerKw.HasValue)
            .WithMessage("Power must be greater than 0.");

        RuleFor(x => x.FuelTankCapacity)
            .GreaterThan(0).When(x => x.FuelTankCapacity.HasValue)
            .WithMessage("Fuel tank capacity must be greater than 0.");

        RuleFor(x => x.GrossVehicleWeightKg)
            .GreaterThan(0).When(x => x.GrossVehicleWeightKg.HasValue)
            .WithMessage("Gross vehicle weight must be greater than 0.");

        RuleFor(x => x.KerbWeightKg)
            .GreaterThan(0).When(x => x.KerbWeightKg.HasValue)
            .WithMessage("Kerb weight must be greater than 0.");

        RuleFor(x => x.PayloadCapacityKg)
            .GreaterThan(0).When(x => x.PayloadCapacityKg.HasValue)
            .WithMessage("Payload capacity must be greater than 0.");

        RuleFor(x => x.AxleCount)
            .GreaterThan(0).When(x => x.AxleCount.HasValue)
            .WithMessage("Axle count must be greater than 0.");

        RuleFor(x => x.EmissionStandard)
            .MaximumLength(50).WithMessage("Emission standard cannot exceed 50 characters.");
    }
}

public sealed class CreateVehicleRegistrationRequestValidator : AbstractValidator<CreateVehicleRegistrationRequest>
{
    public CreateVehicleRegistrationRequestValidator()
    {
        RuleFor(x => x.RegistrationNumber)
            .NotEmpty().WithMessage("Registration number is required.")
            .MaximumLength(50).WithMessage("Registration number cannot exceed 50 characters.");

        RuleFor(x => x.RegistrationStateProvince)
            .MaximumLength(50).WithMessage("Registration state/province cannot exceed 50 characters.");

        RuleFor(x => x.RegistrationCountryCode)
            .MaximumLength(10).WithMessage("Registration country code cannot exceed 10 characters.");

        RuleFor(x => x.IssuingAuthority)
            .MaximumLength(150).WithMessage("Issuing authority cannot exceed 150 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");

        RuleFor(x => x)
            .Must(x => !x.RegistrationDate.HasValue || !x.ExpiryDate.HasValue || x.ExpiryDate >= x.RegistrationDate)
            .WithMessage("Registration expiry date cannot be earlier than registration date.");
    }
}

public sealed class CreateVehicleNoteRequestValidator : AbstractValidator<CreateVehicleNoteRequest>
{
    public CreateVehicleNoteRequestValidator()
    {
        RuleFor(x => x.NoteText)
            .NotEmpty().WithMessage("Note text is required.")
            .MaximumLength(2000).WithMessage("Note text cannot exceed 2000 characters.");
    }
}
