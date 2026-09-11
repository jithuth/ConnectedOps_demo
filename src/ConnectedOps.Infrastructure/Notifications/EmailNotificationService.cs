using ConnectedOps.Application.Notifications;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Notifications;

public sealed class EmailNotificationService : IEmailNotificationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        ConnectedOpsDbContext dbContext,
        ILogger<EmailNotificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default)
    {
        // Log transactional email delivery
        _logger.LogInformation(
            "Sending Email: [To: {To}] [Subject: {Subject}]",
            message.To,
            message.Subject);

        // In development / testing environment, output is logged and ready for SMTP/SES integration
        return Task.CompletedTask;
    }

    public async Task SendInvoiceReadyEmailAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);

        if (invoice is null || string.IsNullOrWhiteSpace(invoice.BillingEmail))
            return;

        var itemsHtml = string.Join("", invoice.Items.Select(item =>
            $"<tr><td style='padding:8px 12px;border-bottom:1px solid #e5e7eb;'>{item.Description}</td>" +
            $"<td style='padding:8px 12px;border-bottom:1px solid #e5e7eb;text-align:center;'>{item.Quantity:G29}</td>" +
            $"<td style='padding:8px 12px;border-bottom:1px solid #e5e7eb;text-align:right;'>{invoice.Currency} {item.UnitPrice:N2}</td>" +
            $"<td style='padding:8px 12px;border-bottom:1px solid #e5e7eb;text-align:right;'>{invoice.Currency} {item.LineTotal:N2}</td></tr>"));

        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background-color:#f9fafb;margin:0;padding:24px;color:#111827;'>
  <div style='max-width:600px;margin:0 auto;background:#ffffff;border-radius:12px;border:1px solid #e5e7eb;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);'>
    <div style='background:#2563eb;padding:24px;color:#ffffff;'>
      <h1 style='margin:0;font-size:22px;'>ConnectedOps Billing</h1>
      <p style='margin:4px 0 0 0;font-size:14px;opacity:0.9;'>New Invoice #{invoice.InvoiceNumber}</p>
    </div>
    <div style='padding:24px;'>
      <p>Hello <strong>{invoice.BillingContactName ?? "Valued Customer"}</strong>,</p>
      <p>A new invoice has been issued for your ConnectedOps Fleet & Asset Intelligence account.</p>
      
      <div style='background:#f3f4f6;border-radius:8px;padding:16px;margin:20px 0;'>
        <table style='width:100%;font-size:14px;'>
          <tr><td><strong>Invoice Number:</strong></td><td style='text-align:right;'>{invoice.InvoiceNumber}</td></tr>
          <tr><td><strong>Issue Date:</strong></td><td style='text-align:right;'>{invoice.IssueDateUtc:MMM dd, yyyy}</td></tr>
          <tr><td><strong>Due Date:</strong></td><td style='text-align:right;'>{invoice.DueDateUtc:MMM dd, yyyy}</td></tr>
          <tr><td><strong>Total Amount:</strong></td><td style='text-align:right;font-size:16px;color:#2563eb;font-weight:bold;'>{invoice.Currency} {invoice.TotalAmount:N2}</td></tr>
        </table>
      </div>

      <table style='width:100%;border-collapse:collapse;font-size:13px;margin-bottom:20px;'>
        <thead>
          <tr style='background:#f9fafb;text-align:left;'>
            <th style='padding:8px 12px;border-bottom:2px solid #e5e7eb;'>Description</th>
            <th style='padding:8px 12px;border-bottom:2px solid #e5e7eb;text-align:center;'>Qty</th>
            <th style='padding:8px 12px;border-bottom:2px solid #e5e7eb;text-align:right;'>Rate</th>
            <th style='padding:8px 12px;border-bottom:2px solid #e5e7eb;text-align:right;'>Amount</th>
          </tr>
        </thead>
        <tbody>
          {itemsHtml}
        </tbody>
      </table>

      <p style='color:#6b7280;font-size:13px;'>Thank you for choosing ConnectedOps for your fleet intelligence operations.</p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(new EmailMessageDto(
            invoice.BillingEmail,
            $"Invoice {invoice.InvoiceNumber} from ConnectedOps",
            html), cancellationToken);
    }

    public async Task SendPaymentReceiptEmailAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.PaymentTransactions
            .Include(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

        if (payment is null || string.IsNullOrWhiteSpace(payment.PayerEmail))
            return;

        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background-color:#f9fafb;margin:0;padding:24px;color:#111827;'>
  <div style='max-width:600px;margin:0 auto;background:#ffffff;border-radius:12px;border:1px solid #e5e7eb;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);'>
    <div style='background:#059669;padding:24px;color:#ffffff;'>
      <h1 style='margin:0;font-size:22px;'>Payment Confirmation</h1>
      <p style='margin:4px 0 0 0;font-size:14px;opacity:0.9;'>Receipt #{payment.TransactionReference}</p>
    </div>
    <div style='padding:24px;'>
      <p>Thank you for your payment. We have successfully processed your transaction.</p>
      
      <div style='background:#f3f4f6;border-radius:8px;padding:16px;margin:20px 0;'>
        <table style='width:100%;font-size:14px;'>
          <tr><td><strong>Reference:</strong></td><td style='text-align:right;'>{payment.TransactionReference}</td></tr>
          <tr><td><strong>Date:</strong></td><td style='text-align:right;'>{payment.ProcessedAtUtc:MMM dd, yyyy HH:mm} UTC</td></tr>
          <tr><td><strong>Method:</strong></td><td style='text-align:right;'>{payment.PaymentMethod} {(payment.LastFourDigits != null ? $"ending in {payment.LastFourDigits}" : "")}</td></tr>
          <tr><td><strong>Amount Paid:</strong></td><td style='text-align:right;font-size:16px;color:#059669;font-weight:bold;'>{payment.Currency} {payment.Amount:N2}</td></tr>
        </table>
      </div>

      <p style='color:#6b7280;font-size:13px;'>If you have any questions regarding this receipt, please contact ConnectedOps customer support.</p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(new EmailMessageDto(
            payment.PayerEmail,
            $"Payment Receipt - {payment.TransactionReference}",
            html), cancellationToken);
    }

    public async Task SendSubscriptionRenewalReminderAsync(Guid subscriptionId, int daysRemaining, CancellationToken cancellationToken = default)
    {
        var sub = await _dbContext.TenantSubscriptions
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

        if (sub is null)
            return;

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == sub.TenantId, cancellationToken);
        if (tenant is null || string.IsNullOrWhiteSpace(tenant.Email))
            return;

        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background-color:#f9fafb;margin:0;padding:24px;color:#111827;'>
  <div style='max-width:600px;margin:0 auto;background:#ffffff;border-radius:12px;border:1px solid #e5e7eb;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);'>
    <div style='background:#2563eb;padding:24px;color:#ffffff;'>
      <h1 style='margin:0;font-size:22px;'>ConnectedOps Subscription Renewal</h1>
      <p style='margin:4px 0 0 0;font-size:14px;opacity:0.9;'>Action Recommended</p>
    </div>
    <div style='padding:24px;'>
      <p>Hello <strong>{tenant.Name}</strong> team,</p>
      <p>Your <strong>{sub.Plan?.Name}</strong> plan subscription is scheduled to renew in <strong>{daysRemaining} day(s)</strong> on <strong>{sub.CurrentPeriodEndUtc:MMM dd, yyyy}</strong>.</p>
      
      <div style='background:#f3f4f6;border-radius:8px;padding:16px;margin:20px 0;'>
        <table style='width:100%;font-size:14px;'>
          <tr><td><strong>Plan:</strong></td><td style='text-align:right;'>{sub.Plan?.Name}</td></tr>
          <tr><td><strong>Billing Interval:</strong></td><td style='text-align:right;'>{sub.Plan?.BillingInterval}</td></tr>
          <tr><td><strong>Renewal Rate:</strong></td><td style='text-align:right;font-weight:bold;'>{sub.Plan?.Currency} {sub.Plan?.Price:N2}</td></tr>
          <tr><td><strong>Auto-Renew:</strong></td><td style='text-align:right;'>{(sub.AutoRenew ? "Enabled" : "Disabled")}</td></tr>
        </table>
      </div>

      <p style='color:#6b7280;font-size:13px;'>You can view or adjust your subscription anytime in your ConnectedOps Billing portal.</p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(new EmailMessageDto(
            tenant.Email,
            $"Your ConnectedOps Subscription Renews in {daysRemaining} Days",
            html), cancellationToken);
    }

    public async Task SendPaymentOverdueReminderAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        if (invoice is null || string.IsNullOrWhiteSpace(invoice.BillingEmail))
            return;

        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background-color:#f9fafb;margin:0;padding:24px;color:#111827;'>
  <div style='max-width:600px;margin:0 auto;background:#ffffff;border-radius:12px;border:1px solid #e5e7eb;overflow:hidden;'>
    <div style='background:#dc2626;padding:24px;color:#ffffff;'>
      <h1 style='margin:0;font-size:22px;'>Payment Reminder: Invoice Overdue</h1>
      <p style='margin:4px 0 0 0;font-size:14px;'>Invoice #{invoice.InvoiceNumber}</p>
    </div>
    <div style='padding:24px;'>
      <p>Hello <strong>{invoice.BillingContactName ?? "Valued Customer"}</strong>,</p>
      <p>This is a reminder that payment for Invoice #{invoice.InvoiceNumber} was due on <strong>{invoice.DueDateUtc:MMM dd, yyyy}</strong> and is currently overdue.</p>
      
      <div style='background:#fef2f2;border-left:4px solid #dc2626;padding:16px;margin:20px 0;'>
        <p style='margin:0;font-size:14px;color:#991b1b;'><strong>Outstanding Balance:</strong> {invoice.Currency} {invoice.BalanceDue:N2}</p>
      </div>

      <p style='color:#6b7280;font-size:13px;'>Please log in to your ConnectedOps portal to settle the outstanding balance.</p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(new EmailMessageDto(
            invoice.BillingEmail,
            $"Overdue Payment Reminder: Invoice {invoice.InvoiceNumber}",
            html), cancellationToken);
    }
}
