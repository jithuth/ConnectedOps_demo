using FluentValidation;

namespace ConnectedOps.Application.Assets.Validators;

public sealed class CreateAssetCategoryRequestValidator : AbstractValidator<CreateAssetCategoryRequest>
{
    public CreateAssetCategoryRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Category code is required.")
            .MaximumLength(50).WithMessage("Category code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}

public sealed class UpdateAssetCategoryRequestValidator : AbstractValidator<UpdateAssetCategoryRequest>
{
    public UpdateAssetCategoryRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Category code is required.")
            .MaximumLength(50).WithMessage("Category code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}

public sealed class CreateAssetTypeRequestValidator : AbstractValidator<CreateAssetTypeRequest>
{
    public CreateAssetTypeRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Type code is required.")
            .MaximumLength(50).WithMessage("Type code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Type name is required.")
            .MaximumLength(100).WithMessage("Type name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}

public sealed class UpdateAssetTypeRequestValidator : AbstractValidator<UpdateAssetTypeRequest>
{
    public UpdateAssetTypeRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Type code is required.")
            .MaximumLength(50).WithMessage("Type code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Type name is required.")
            .MaximumLength(100).WithMessage("Type name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}

public sealed class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.AssetNumber)
            .NotEmpty().WithMessage("Asset number is required.")
            .MaximumLength(50).WithMessage("Asset number cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Asset name is required.")
            .MaximumLength(150).WithMessage("Asset name cannot exceed 150 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.AssetTypeId)
            .NotEmpty().WithMessage("Asset type is required.");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).WithMessage("Serial number cannot exceed 100 characters.");

        RuleFor(x => x.InternalCode)
            .MaximumLength(50).WithMessage("Internal code cannot exceed 50 characters.");

        RuleFor(x => x.Make)
            .MaximumLength(100).WithMessage("Make cannot exceed 100 characters.");

        RuleFor(x => x.Model)
            .MaximumLength(100).WithMessage("Model cannot exceed 100 characters.");

        RuleFor(x => x.Year)
            .MaximumLength(10).WithMessage("Year cannot exceed 10 characters.");

        RuleFor(x => x.PurchaseCost)
            .GreaterThanOrEqualTo(0).When(x => x.PurchaseCost.HasValue)
            .WithMessage("Purchase cost cannot be negative.");
    }
}

public sealed class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Asset name is required.")
            .MaximumLength(150).WithMessage("Asset name cannot exceed 150 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.AssetTypeId)
            .NotEmpty().WithMessage("Asset type is required.");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).WithMessage("Serial number cannot exceed 100 characters.");

        RuleFor(x => x.InternalCode)
            .MaximumLength(50).WithMessage("Internal code cannot exceed 50 characters.");

        RuleFor(x => x.Make)
            .MaximumLength(100).WithMessage("Make cannot exceed 100 characters.");

        RuleFor(x => x.Model)
            .MaximumLength(100).WithMessage("Model cannot exceed 100 characters.");

        RuleFor(x => x.PurchaseCost)
            .GreaterThanOrEqualTo(0).When(x => x.PurchaseCost.HasValue)
            .WithMessage("Purchase cost cannot be negative.");
    }
}

public sealed class ChangeAssetStatusRequestValidator : AbstractValidator<ChangeAssetStatusRequest>
{
    public ChangeAssetStatusRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason for status change is required.")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}

public sealed class AssignAssetToEmployeeRequestValidator : AbstractValidator<AssignAssetToEmployeeRequest>
{
    public AssignAssetToEmployeeRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset is required.");

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee is required.");
    }
}

public sealed class AssignAssetToVehicleRequestValidator : AbstractValidator<AssignAssetToVehicleRequest>
{
    public AssignAssetToVehicleRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset is required.");

        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle is required.");
    }
}

public sealed class StartAssetUsageSessionRequestValidator : AbstractValidator<StartAssetUsageSessionRequest>
{
    public StartAssetUsageSessionRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset is required.");

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee is required.");

        RuleFor(x => x.MeterHours)
            .GreaterThanOrEqualTo(0).When(x => x.MeterHours.HasValue)
            .WithMessage("Meter hours cannot be negative.");
    }
}

public sealed class EndAssetUsageSessionRequestValidator : AbstractValidator<EndAssetUsageSessionRequest>
{
    public EndAssetUsageSessionRequestValidator()
    {
        RuleFor(x => x.MeterHours)
            .GreaterThanOrEqualTo(0).When(x => x.MeterHours.HasValue)
            .WithMessage("Meter hours cannot be negative.");
    }
}

public sealed class CreateAssetTransferRequestValidator : AbstractValidator<CreateAssetTransferRequest>
{
    public CreateAssetTransferRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}

public sealed class CreateAssetInspectionRequestValidator : AbstractValidator<CreateAssetInspectionRequest>
{
    public CreateAssetInspectionRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset is required.");

        RuleFor(x => x.InspectionDateUtc)
            .NotEmpty().WithMessage("Inspection date is required.");
    }
}

public sealed class CreateAssetCalibrationRecordRequestValidator : AbstractValidator<CreateAssetCalibrationRecordRequest>
{
    public CreateAssetCalibrationRecordRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset is required.");

        RuleFor(x => x.PerformedBy)
            .NotEmpty().WithMessage("Performed by is required.")
            .MaximumLength(150).WithMessage("Performed by cannot exceed 150 characters.");

        RuleFor(x => x.CalibrationDateUtc)
            .NotEmpty().WithMessage("Calibration date is required.");

        RuleFor(x => x.NextCalibrationDueUtc)
            .NotEmpty().WithMessage("Next calibration due date is required.")
            .GreaterThanOrEqualTo(x => x.CalibrationDateUtc)
            .WithMessage("Next calibration due date cannot be earlier than calibration date.");
    }
}

public sealed class CreateAssetConditionRecordRequestValidator : AbstractValidator<CreateAssetConditionRecordRequest>
{
    public CreateAssetConditionRecordRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}

public sealed class CreateAssetDocumentRequestValidator : AbstractValidator<CreateAssetDocumentRequest>
{
    public CreateAssetDocumentRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset is required.");

        RuleFor(x => x.DocumentName)
            .NotEmpty().WithMessage("Document name is required.")
            .MaximumLength(200).WithMessage("Document name cannot exceed 200 characters.");

        RuleFor(x => x.StorageKey)
            .NotEmpty().WithMessage("Storage key is required.")
            .MaximumLength(500).WithMessage("Storage key cannot exceed 500 characters.");
    }
}

public sealed class CreateAssetIdentifierRequestValidator : AbstractValidator<CreateAssetIdentifierRequest>
{
    public CreateAssetIdentifierRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset is required.");

        RuleFor(x => x.IdentifierValue)
            .NotEmpty().WithMessage("Identifier value is required.")
            .MaximumLength(200).WithMessage("Identifier value cannot exceed 200 characters.");
    }
}
