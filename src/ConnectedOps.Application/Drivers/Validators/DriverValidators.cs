using FluentValidation;

namespace ConnectedOps.Application.Drivers.Validators;

public sealed class CreateDriverRequestValidator : AbstractValidator<CreateDriverRequest>
{
    public CreateDriverRequestValidator()
    {
        RuleFor(x => x.DriverNumber)
            .NotEmpty().WithMessage("Driver number is required.")
            .MaximumLength(50).WithMessage("Driver number cannot exceed 50 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email format is invalid.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters.");

        RuleFor(x => x.NationalityCode)
            .MaximumLength(10).WithMessage("Nationality code cannot exceed 10 characters.");

        RuleFor(x => x.PreferredLanguage)
            .MaximumLength(20).WithMessage("Preferred language cannot exceed 20 characters.");

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth cannot be in the future.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date cannot be earlier than start date.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.");
    }
}

public sealed class UpdateDriverRequestValidator : AbstractValidator<UpdateDriverRequest>
{
    public UpdateDriverRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email format is invalid.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date cannot be earlier than start date.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.");
    }
}

public sealed class ChangeDriverStatusRequestValidator : AbstractValidator<ChangeDriverStatusRequest>
{
    public ChangeDriverStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid driver status.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public sealed class CreateDriverEmergencyContactRequestValidator : AbstractValidator<CreateDriverEmergencyContactRequest>
{
    public CreateDriverEmergencyContactRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Contact name is required.")
            .MaximumLength(150).WithMessage("Contact name cannot exceed 150 characters.");

        RuleFor(x => x.Relationship)
            .NotEmpty().WithMessage("Relationship is required.")
            .MaximumLength(50).WithMessage("Relationship cannot exceed 50 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.AlternatePhone)
            .MaximumLength(50).WithMessage("Alternate phone cannot exceed 50 characters.");
    }
}

public sealed class CreateDriverNoteRequestValidator : AbstractValidator<CreateDriverNoteRequest>
{
    public CreateDriverNoteRequestValidator()
    {
        RuleFor(x => x.NoteText)
            .NotEmpty().WithMessage("Note text is required.")
            .MaximumLength(2000).WithMessage("Note text cannot exceed 2000 characters.");
    }
}
