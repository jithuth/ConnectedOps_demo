using FluentValidation;

namespace ConnectedOps.Application.Drivers.Validators;

public sealed class CreateDriverDocumentRequestValidator : AbstractValidator<CreateDriverDocumentRequest>
{
    public CreateDriverDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.DocumentNumber)
            .MaximumLength(100).WithMessage("Document number cannot exceed 100 characters.");

        RuleFor(x => x.IssuingAuthority)
            .MaximumLength(150).WithMessage("Issuing authority cannot exceed 150 characters.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.IssueDate!.Value)
            .When(x => x.IssueDate.HasValue && x.ExpiryDate.HasValue)
            .WithMessage("Expiry date cannot be earlier than issue date.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public sealed class UpdateDriverDocumentRequestValidator : AbstractValidator<UpdateDriverDocumentRequest>
{
    public UpdateDriverDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.DocumentNumber)
            .MaximumLength(100).WithMessage("Document number cannot exceed 100 characters.");

        RuleFor(x => x.IssuingAuthority)
            .MaximumLength(150).WithMessage("Issuing authority cannot exceed 150 characters.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.IssueDate!.Value)
            .When(x => x.IssueDate.HasValue && x.ExpiryDate.HasValue)
            .WithMessage("Expiry date cannot be earlier than issue date.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}
