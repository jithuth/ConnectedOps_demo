using FluentValidation;

namespace ConnectedOps.Application.Safety.Validators;

public sealed class CreateSafetyIncidentRequestValidator : AbstractValidator<CreateSafetyIncidentRequest>
{
    public CreateSafetyIncidentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Incident title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Incident description is required.");
    }
}

public sealed class UpdateSafetyIncidentRequestValidator : AbstractValidator<UpdateSafetyIncidentRequest>
{
    public UpdateSafetyIncidentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Incident title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Incident description is required.");
    }
}

public sealed class CompleteSafetyInvestigationRequestValidator : AbstractValidator<CompleteSafetyInvestigationRequest>
{
    public CompleteSafetyInvestigationRequestValidator()
    {
        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("Investigation summary is required.");
    }
}

public sealed class CreateSafetyViolationRequestValidator : AbstractValidator<CreateSafetyViolationRequest>
{
    public CreateSafetyViolationRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Violation description is required.")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}

public sealed class CreateCorrectiveActionRequestValidator : AbstractValidator<CreateCorrectiveActionRequest>
{
    public CreateCorrectiveActionRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Action title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Action description is required.");
    }
}
