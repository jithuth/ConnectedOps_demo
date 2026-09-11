using FluentValidation;

namespace ConnectedOps.Application.Accounting;

public sealed class CreateJournalEntryValidator : AbstractValidator<CreateJournalEntryRequest>
{
    public CreateJournalEntryValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Journal entry description is required.")
            .MaximumLength(250).WithMessage("Description cannot exceed 250 characters.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Journal entry must have lines.")
            .Must(lines => lines.Count >= 2).WithMessage("Journal entry must contain at least 2 lines (double-entry).")
            .Must(lines =>
            {
                var totalDebits = lines.Sum(l => l.DebitAmount);
                var totalCredits = lines.Sum(l => l.CreditAmount);
                return Math.Abs(totalDebits - totalCredits) < 0.001m;
            }).WithMessage("Total debits must equal total credits in a balanced journal entry.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId)
                .NotEmpty().WithMessage("AccountId is required.");

            line.RuleFor(l => l.DebitAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Debit amount cannot be negative.");

            line.RuleFor(l => l.CreditAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Credit amount cannot be negative.");

            line.RuleFor(l => l)
                .Must(l => (l.DebitAmount > 0 && l.CreditAmount == 0) || (l.CreditAmount > 0 && l.DebitAmount == 0))
                .WithMessage("Each line must specify either a Debit amount or a Credit amount, not both.");
        });
    }
}
