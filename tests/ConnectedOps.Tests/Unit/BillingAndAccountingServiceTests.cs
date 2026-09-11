using ConnectedOps.Application.Accounting;
using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Accounting;
using ConnectedOps.Domain.Billing;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Accounting;
using ConnectedOps.Infrastructure.Billing;
using ConnectedOps.Infrastructure.Notifications;
using ConnectedOps.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class BillingAndAccountingServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private (TestUserContext userContext, AccountingLedgerService accountingService, InvoiceService invoiceService, PaymentService paymentService, TenantSubscriptionService subService)
        CreateServices(Infrastructure.Persistence.ConnectedOpsDbContext context)
    {
        var userContext = new TestUserContext
        {
            TenantId = _tenantId,
            UserId = _userId
        };

        var emailService = new EmailNotificationService(context, NullLogger<EmailNotificationService>.Instance);
        var accountingService = new AccountingLedgerService(context, userContext);
        var invoiceService = new InvoiceService(
            context,
            userContext,
            accountingService,
            emailService,
            NullLogger<InvoiceService>.Instance);

        var paymentService = new PaymentService(
            context,
            userContext,
            accountingService,
            emailService,
            NullLogger<PaymentService>.Instance);

        var subService = new TenantSubscriptionService(context, userContext);

        return (userContext, accountingService, invoiceService, paymentService, subService);
    }

    [Fact]
    public async Task AccountingLedgerService_EnsuresDefaultChartOfAccounts()
    {
        using var context = TestDbContextFactory.Create();
        var (_, accountingService, _, _, _) = CreateServices(context);

        var accounts = await accountingService.GetAccountsAsync();

        Assert.NotEmpty(accounts);
        Assert.Contains(accounts, a => a.AccountCode == "1000" && a.Category == AccountCategory.Asset); // Cash
        Assert.Contains(accounts, a => a.AccountCode == "1200" && a.Category == AccountCategory.Asset); // A/R
        Assert.Contains(accounts, a => a.AccountCode == "4000" && a.Category == AccountCategory.Revenue); // SaaS Revenue
        Assert.Contains(accounts, a => a.AccountCode == "5000" && a.Category == AccountCategory.Expense); // Fleet Expense
    }

    [Fact]
    public async Task AccountingLedgerService_RejectsUnbalancedJournalEntry()
    {
        using var context = TestDbContextFactory.Create();
        var (_, accountingService, _, _, _) = CreateServices(context);

        var accounts = await accountingService.GetAccountsAsync();
        var cashAcc = accounts.First(a => a.AccountCode == "1000");
        var revAcc = accounts.First(a => a.AccountCode == "4000");

        var request = new CreateJournalEntryRequest
        {
            Description = "Unbalanced test transaction",
            Lines =
            [
                new JournalEntryLineRequest { AccountId = cashAcc.Id, DebitAmount = 100m, CreditAmount = 0 },
                new JournalEntryLineRequest { AccountId = revAcc.Id, DebitAmount = 0, CreditAmount = 80m } // Debits != Credits
            ]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            accountingService.RecordJournalEntryAsync(request));
    }

    [Fact]
    public async Task InvoiceService_CreatesDraftInvoice_CalculatesTaxesAndTotalsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var (_, _, invoiceService, _, _) = CreateServices(context);

        var request = new CreateInvoiceRequest
        {
            Title = "Monthly Fleet Telematics Billing",
            DueDateUtc = DateTime.UtcNow.AddDays(30),
            Currency = "USD",
            BillingContactName = "Global Logistics LLC",
            BillingEmail = "finance@globallogistics.com",
            Items =
            [
                new CreateInvoiceItemRequest
                {
                    Description = "GPS Tracker Subscription (20 units)",
                    Quantity = 20,
                    UnitPrice = 15.00m,
                    TaxRatePercentage = 10.0m // 10% of $300 = $30
                },
                new CreateInvoiceItemRequest
                {
                    Description = "Fleet Analytics Addon",
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    TaxRatePercentage = 0m // $50
                }
            ]
        };

        var invoice = await invoiceService.CreateInvoiceAsync(request);

        Assert.NotNull(invoice);
        Assert.StartsWith("INV-", invoice.InvoiceNumber);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Equal(350.00m, invoice.SubTotal); // 300 + 50
        Assert.Equal(30.00m, invoice.TaxAmount); // 30
        Assert.Equal(380.00m, invoice.TotalAmount); // 350 + 30
        Assert.Equal(0m, invoice.AmountPaid);
        Assert.Equal(380.00m, invoice.BalanceDue);
        Assert.Equal(2, invoice.Items.Count);
    }

    [Fact]
    public async Task InvoiceService_IssueInvoice_PostsToLedgerAndUpdatesStatus()
    {
        using var context = TestDbContextFactory.Create();
        var (_, accountingService, invoiceService, _, _) = CreateServices(context);

        var created = await invoiceService.CreateInvoiceAsync(new CreateInvoiceRequest
        {
            Title = "Annual SaaS Subscription",
            DueDateUtc = DateTime.UtcNow.AddDays(15),
            Items =
            [
                new CreateInvoiceItemRequest
                {
                    Description = "Professional Plan Annual",
                    Quantity = 1,
                    UnitPrice = 1200.00m,
                    TaxRatePercentage = 0m
                }
            ]
        });

        var issued = await invoiceService.IssueInvoiceAsync(created.Id);

        Assert.Equal(InvoiceStatus.Issued, issued.Status);

        // Verify ledger entries were posted: Debit 1200 A/R, Credit 1200 Revenue
        var entries = await accountingService.GetEntriesAsync();
        Assert.Contains(entries, e => e.AccountCode == "1200" && e.DebitAmount == 1200.00m);
        Assert.Contains(entries, e => e.AccountCode == "4000" && e.CreditAmount == 1200.00m);
    }

    [Fact]
    public async Task PaymentService_RecordsPayment_AppliesToInvoice_UpdatesBalanceAndPostsToLedger()
    {
        using var context = TestDbContextFactory.Create();
        var (_, accountingService, invoiceService, paymentService, _) = CreateServices(context);

        // 1. Create and Issue Invoice for $500
        var invoice = await invoiceService.CreateInvoiceAsync(new CreateInvoiceRequest
        {
            Title = "Quarterly Fleet Tracking",
            DueDateUtc = DateTime.UtcNow.AddDays(30),
            Items =
            [
                new CreateInvoiceItemRequest
                {
                    Description = "Telematics Access",
                    Quantity = 1,
                    UnitPrice = 500.00m,
                    TaxRatePercentage = 0m
                }
            ]
        });
        await invoiceService.IssueInvoiceAsync(invoice.Id);

        // 2. Record Partial Payment of $200
        var payment = await paymentService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = invoice.Id,
            Amount = 200.00m,
            PaymentMethod = PaymentMethod.CreditCard,
            CardBrand = "Visa",
            LastFourDigits = "4242",
            Notes = "First installment"
        });

        Assert.NotNull(payment);
        Assert.StartsWith("TXN-", payment.TransactionReference);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(200.00m, payment.Amount);

        // 3. Verify Invoice status is now PartiallyPaid
        var refreshedInvoice = await invoiceService.GetInvoiceByIdAsync(invoice.Id);
        Assert.NotNull(refreshedInvoice);
        Assert.Equal(InvoiceStatus.PartiallyPaid, refreshedInvoice.Status);
        Assert.Equal(200.00m, refreshedInvoice.AmountPaid);
        Assert.Equal(300.00m, refreshedInvoice.BalanceDue);

        // 4. Record Final Payment of $300
        await paymentService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = invoice.Id,
            Amount = 300.00m,
            PaymentMethod = PaymentMethod.BankTransfer,
            Notes = "Final payment"
        });

        var fullyPaidInvoice = await invoiceService.GetInvoiceByIdAsync(invoice.Id);
        Assert.NotNull(fullyPaidInvoice);
        Assert.Equal(InvoiceStatus.Paid, fullyPaidInvoice.Status);
        Assert.Equal(500.00m, fullyPaidInvoice.AmountPaid);
        Assert.Equal(0m, fullyPaidInvoice.BalanceDue);

        // 5. Verify Ledger: Debit Cash 1000 ($200 + $300), Credit A/R 1200 ($200 + $300)
        var entries = await accountingService.GetEntriesAsync();
        var cashDebits = entries.Where(e => e.AccountCode == "1000").Sum(e => e.DebitAmount);
        var arCredits = entries.Where(e => e.AccountCode == "1200").Sum(e => e.CreditAmount);
        Assert.Equal(500.00m, cashDebits);
        Assert.Equal(500.00m, arCredits);
    }

    [Fact]
    public async Task TenantSubscriptionService_ChangesPlanSuccessfully()
    {
        using var context = TestDbContextFactory.Create();
        var (_, _, _, _, subService) = CreateServices(context);

        // Seed 2 plans
        var starterPlan = new SubscriptionPlan("Starter Fleet", "STARTER", 49.00m, BillingInterval.Monthly, maxVehicles: 10, maxUsers: 3, maxAssets: 25);
        var proPlan = new SubscriptionPlan("Professional Fleet", "PRO", 149.00m, BillingInterval.Monthly, maxVehicles: 50, maxUsers: 15, maxAssets: 150);
        context.SubscriptionPlans.AddRange(starterPlan, proPlan);

        var tenantSub = new TenantSubscription(
            _tenantId,
            starterPlan.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1),
            SubscriptionStatus.Active);
        context.TenantSubscriptions.Add(tenantSub);
        await context.SaveChangesAsync();

        // Check initial
        var initial = await subService.GetCurrentSubscriptionAsync();
        Assert.NotNull(initial);
        Assert.Equal("STARTER", initial.PlanCode);
        Assert.Equal(10, initial.EffectiveMaxVehicles);

        // Switch to Pro
        var switched = await subService.ChangePlanAsync(new ChangeSubscriptionPlanRequest { NewPlanId = proPlan.Id });
        Assert.NotNull(switched);
        Assert.Equal("PRO", switched.PlanCode);
        Assert.Equal(50, switched.EffectiveMaxVehicles);
        Assert.Equal(15, switched.EffectiveMaxUsers);
        Assert.Equal(150, switched.EffectiveMaxAssets);
    }
}
