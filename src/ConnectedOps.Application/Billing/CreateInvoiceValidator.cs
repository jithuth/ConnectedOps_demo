using FluentValidation;

namespace ConnectedOps.Application.Billing;

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Invoice title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency code must be 3 characters.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Invoice must contain at least one line item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Item description is required.")
                .MaximumLength(250).WithMessage("Item description cannot exceed 250 characters.");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Item quantity must be greater than zero.");

            item.RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Item unit price cannot be negative.");

            item.RuleFor(x => x.TaxRatePercentage)
                .GreaterThanOrEqualTo(0).WithMessage("Tax rate cannot be negative.");
        });
    }
}
