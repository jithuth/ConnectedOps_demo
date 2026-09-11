using FluentValidation;

namespace ConnectedOps.Application.Organization.Validators;

public sealed class CreateTenantUserAdminRequestValidator : AbstractValidator<CreateTenantUserAdminRequest>
{
    public CreateTenantUserAdminRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.")
            .MaximumLength(250).WithMessage("Email cannot exceed 250 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.JobTitle)
            .MaximumLength(150).WithMessage("Job title cannot exceed 150 characters.");
    }
}

public sealed class UpdateTenantUserAdminRequestValidator : AbstractValidator<UpdateTenantUserAdminRequest>
{
    public UpdateTenantUserAdminRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");
    }
}
