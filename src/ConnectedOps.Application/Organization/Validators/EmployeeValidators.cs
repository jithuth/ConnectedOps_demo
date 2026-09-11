using FluentValidation;

namespace ConnectedOps.Application.Organization.Validators;

public sealed class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeNumber)
            .NotEmpty().WithMessage("Employee number is required.")
            .MaximumLength(50).WithMessage("Employee number cannot exceed 50 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.")
            .MaximumLength(250).WithMessage("Email cannot exceed 250 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");

        RuleFor(x => x.JobTitle)
            .MaximumLength(150).WithMessage("Job title cannot exceed 150 characters.");

        RuleFor(x => x.EmploymentType)
            .IsInEnum().WithMessage("Valid employment type is required.");

        RuleFor(x => x.EmploymentStatus)
            .IsInEnum().WithMessage("Valid employment status is required.");
    }
}

public sealed class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeNumber)
            .NotEmpty().WithMessage("Employee number is required.")
            .MaximumLength(50).WithMessage("Employee number cannot exceed 50 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.")
            .MaximumLength(250).WithMessage("Email cannot exceed 250 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");

        RuleFor(x => x.JobTitle)
            .MaximumLength(150).WithMessage("Job title cannot exceed 150 characters.");

        RuleFor(x => x.EmploymentType)
            .IsInEnum().WithMessage("Valid employment type is required.");

        RuleFor(x => x.EmploymentStatus)
            .IsInEnum().WithMessage("Valid employment status is required.");
    }
}

public sealed class LinkEmployeeUserRequestValidator : AbstractValidator<LinkEmployeeUserRequest>
{
    public LinkEmployeeUserRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
