namespace ConnectedOps.Application.Notifications;

public sealed record EmailMessageDto(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);

public interface IEmailNotificationService
{
    Task SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default);
    Task SendInvoiceReadyEmailAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task SendPaymentReceiptEmailAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task SendSubscriptionRenewalReminderAsync(Guid subscriptionId, int daysRemaining, CancellationToken cancellationToken = default);
    Task SendPaymentOverdueReminderAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
