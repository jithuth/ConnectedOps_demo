namespace ConnectedOps.Application.Billing;

public interface IBillingDashboardService
{
    Task<BillingDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
