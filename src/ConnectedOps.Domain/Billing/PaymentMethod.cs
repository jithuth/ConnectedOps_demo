namespace ConnectedOps.Domain.Billing;

public enum PaymentMethod
{
    CreditCard = 1,
    DebitCard = 2,
    BankTransfer = 3,
    ACH = 4,
    Stripe = 5,
    ManualEntry = 6,
    Other = 99
}
