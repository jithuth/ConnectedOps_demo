using FluentValidation;

namespace ConnectedOps.Application.Accounting;

public sealed class CreateGeneralLedgerAccountValidator : AbstractValidator<CreateGeneralLedgerAccountRequest>
{
    public CreateGeneralLedgerAccountValidator()
    {
        RuleFor(x => x.AccountCode)
            .NotEmpty().WithMessage("Account code is required.")
            .MaximumLength(50).WithMessage("Account code cannot exceed 50 characters.");

        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(150).WithMessage("Account name cannot exceed 150 characters.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid account category.");
    }
}
