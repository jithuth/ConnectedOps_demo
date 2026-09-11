using FluentValidation;

namespace ConnectedOps.Application.Drivers.Validators;

public sealed class CreateDriverCertificationRequestValidator : AbstractValidator<CreateDriverCertificationRequest>
{
    public CreateDriverCertificationRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Certification title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.CertificateNumber)
            .MaximumLength(100).WithMessage("Certificate number cannot exceed 100 characters.");

        RuleFor(x => x.IssuedBy)
            .MaximumLength(150).WithMessage("IssuedBy cannot exceed 150 characters.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.IssueDate!.Value)
            .When(x => x.IssueDate.HasValue && x.ExpiryDate.HasValue)
            .WithMessage("Expiry date cannot be earlier than issue date.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public sealed class UpdateDriverCertificationRequestValidator : AbstractValidator<UpdateDriverCertificationRequest>
{
    public UpdateDriverCertificationRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Certification title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.CertificateNumber)
            .MaximumLength(100).WithMessage("Certificate number cannot exceed 100 characters.");

        RuleFor(x => x.IssuedBy)
            .MaximumLength(150).WithMessage("IssuedBy cannot exceed 150 characters.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.IssueDate!.Value)
            .When(x => x.IssueDate.HasValue && x.ExpiryDate.HasValue)
            .WithMessage("Expiry date cannot be earlier than issue date.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}
