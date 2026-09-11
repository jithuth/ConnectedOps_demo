using FluentValidation;

namespace ConnectedOps.Application.Vehicles.Validators;

public sealed class CreateVehicleDocumentRequestValidator : AbstractValidator<CreateVehicleDocumentRequest>
{
    public CreateVehicleDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("Invalid document type.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required.")
            .MaximumLength(150).WithMessage("Document title cannot exceed 150 characters.");

        RuleFor(x => x.DocumentNumber)
            .MaximumLength(100).WithMessage("Document number cannot exceed 100 characters.");

        RuleFor(x => x.IssuingAuthority)
            .MaximumLength(150).WithMessage("Issuing authority cannot exceed 150 characters.");

        RuleFor(x => x.FileObjectKey)
            .NotEmpty().WithMessage("File object key is required.")
            .MaximumLength(500).WithMessage("File object key cannot exceed 500 characters.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(255).WithMessage("File name cannot exceed 255 characters.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .MaximumLength(100).WithMessage("Content type cannot exceed 100 characters.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than 0 bytes.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");

        RuleFor(x => x)
            .Must(x => !x.IssueDate.HasValue || !x.ExpiryDate.HasValue || x.ExpiryDate >= x.IssueDate)
            .WithMessage("Expiry date cannot be earlier than issue date.");
    }
}

public sealed class UpdateVehicleDocumentRequestValidator : AbstractValidator<UpdateVehicleDocumentRequest>
{
    public UpdateVehicleDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("Invalid document type.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required.")
            .MaximumLength(150).WithMessage("Document title cannot exceed 150 characters.");

        RuleFor(x => x.DocumentNumber)
            .MaximumLength(100).WithMessage("Document number cannot exceed 100 characters.");

        RuleFor(x => x.IssuingAuthority)
            .MaximumLength(150).WithMessage("Issuing authority cannot exceed 150 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");

        RuleFor(x => x)
            .Must(x => !x.IssueDate.HasValue || !x.ExpiryDate.HasValue || x.ExpiryDate >= x.IssueDate)
            .WithMessage("Expiry date cannot be earlier than issue date.");
    }
}
