using FluentValidation;

namespace ConnectedOps.Application.Vehicles.Validators;

public sealed class CreateVehicleMakeRequestValidator : AbstractValidator<CreateVehicleMakeRequest>
{
    public CreateVehicleMakeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Make name is required.")
            .MaximumLength(100).WithMessage("Make name cannot exceed 100 characters.");

        RuleFor(x => x.CountryCode)
            .MaximumLength(10).WithMessage("Country code cannot exceed 10 characters.");
    }
}

public sealed class UpdateVehicleMakeRequestValidator : AbstractValidator<UpdateVehicleMakeRequest>
{
    public UpdateVehicleMakeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Make name is required.")
            .MaximumLength(100).WithMessage("Make name cannot exceed 100 characters.");

        RuleFor(x => x.CountryCode)
            .MaximumLength(10).WithMessage("Country code cannot exceed 10 characters.");
    }
}

public sealed class CreateVehicleModelRequestValidator : AbstractValidator<CreateVehicleModelRequest>
{
    public CreateVehicleModelRequestValidator()
    {
        RuleFor(x => x.VehicleMakeId)
            .NotEmpty().WithMessage("Make is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Model name is required.")
            .MaximumLength(100).WithMessage("Model name cannot exceed 100 characters.");
    }
}

public sealed class UpdateVehicleModelRequestValidator : AbstractValidator<UpdateVehicleModelRequest>
{
    public UpdateVehicleModelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Model name is required.")
            .MaximumLength(100).WithMessage("Model name cannot exceed 100 characters.");
    }
}
