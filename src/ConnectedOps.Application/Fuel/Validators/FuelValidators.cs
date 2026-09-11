using FluentValidation;

namespace ConnectedOps.Application.Fuel.Validators;

public sealed class CreateFuelTransactionRequestValidator : AbstractValidator<CreateFuelTransactionRequest>
{
    public CreateFuelTransactionRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle ID is required.");

        RuleFor(x => x.TransactionDateUtc)
            .NotEmpty().WithMessage("Transaction date is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");

        RuleFor(x => x.TotalCost)
            .GreaterThanOrEqualTo(0).When(x => x.TotalCost.HasValue)
            .WithMessage("Total cost cannot be negative.");

        RuleFor(x => x.OdometerReading)
            .GreaterThanOrEqualTo(0).When(x => x.OdometerReading.HasValue)
            .WithMessage("Odometer reading cannot be negative.");

        RuleFor(x => x.EngineHours)
            .GreaterThanOrEqualTo(0).When(x => x.EngineHours.HasValue)
            .WithMessage("Engine hours cannot be negative.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90 degrees.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180 degrees.");
    }
}

public sealed class UpdateFuelTransactionRequestValidator : AbstractValidator<UpdateFuelTransactionRequest>
{
    public UpdateFuelTransactionRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle ID is required.");

        RuleFor(x => x.TransactionDateUtc)
            .NotEmpty().WithMessage("Transaction date is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");

        RuleFor(x => x.TotalCost)
            .GreaterThanOrEqualTo(0).When(x => x.TotalCost.HasValue)
            .WithMessage("Total cost cannot be negative.");

        RuleFor(x => x.OdometerReading)
            .GreaterThanOrEqualTo(0).When(x => x.OdometerReading.HasValue)
            .WithMessage("Odometer reading cannot be negative.");

        RuleFor(x => x.EngineHours)
            .GreaterThanOrEqualTo(0).When(x => x.EngineHours.HasValue)
            .WithMessage("Engine hours cannot be negative.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90 degrees.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180 degrees.");
    }
}

public sealed class CreateFuelStationRequestValidator : AbstractValidator<CreateFuelStationRequest>
{
    public CreateFuelStationRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Station code is required.")
            .MaximumLength(50).WithMessage("Station code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Station name is required.")
            .MaximumLength(200).WithMessage("Station name cannot exceed 200 characters.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90 degrees.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180 degrees.");
    }
}

public sealed class UpdateFuelStationRequestValidator : AbstractValidator<UpdateFuelStationRequest>
{
    public UpdateFuelStationRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Station code is required.")
            .MaximumLength(50).WithMessage("Station code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Station name is required.")
            .MaximumLength(200).WithMessage("Station name cannot exceed 200 characters.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90 degrees.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180 degrees.");
    }
}

public sealed class CreateFuelCardRequestValidator : AbstractValidator<CreateFuelCardRequest>
{
    public CreateFuelCardRequestValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("Card number is required.");

        RuleFor(x => x.CardReference)
            .NotEmpty().WithMessage("Card reference is required.")
            .MaximumLength(100).WithMessage("Card reference cannot exceed 100 characters.");

        RuleFor(x => x.ProviderName)
            .NotEmpty().WithMessage("Provider name is required.")
            .MaximumLength(200).WithMessage("Provider name cannot exceed 200 characters.");

        RuleFor(x => x.SpendingLimit)
            .GreaterThanOrEqualTo(0).When(x => x.SpendingLimit.HasValue)
            .WithMessage("Spending limit cannot be negative.");
    }
}

public sealed class UpdateFuelCardRequestValidator : AbstractValidator<UpdateFuelCardRequest>
{
    public UpdateFuelCardRequestValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("Card number is required.");

        RuleFor(x => x.CardReference)
            .NotEmpty().WithMessage("Card reference is required.")
            .MaximumLength(100).WithMessage("Card reference cannot exceed 100 characters.");

        RuleFor(x => x.ProviderName)
            .NotEmpty().WithMessage("Provider name is required.")
            .MaximumLength(200).WithMessage("Provider name cannot exceed 200 characters.");

        RuleFor(x => x.SpendingLimit)
            .GreaterThanOrEqualTo(0).When(x => x.SpendingLimit.HasValue)
            .WithMessage("Spending limit cannot be negative.");
    }
}

public sealed class CreateFuelTypeDefinitionRequestValidator : AbstractValidator<CreateFuelTypeDefinitionRequest>
{
    public CreateFuelTypeDefinitionRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Fuel code is required.")
            .MaximumLength(50).WithMessage("Fuel code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Fuel name is required.")
            .MaximumLength(100).WithMessage("Fuel name cannot exceed 100 characters.");

        RuleFor(x => x.Density)
            .GreaterThan(0).When(x => x.Density.HasValue)
            .WithMessage("Fuel density must be greater than zero.");
    }
}

public sealed class AddFuelTransactionDocumentRequestValidator : AbstractValidator<AddFuelTransactionDocumentRequest>
{
    public AddFuelTransactionDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.FileObjectKey)
            .NotEmpty().WithMessage("File object key is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.");
    }
}
