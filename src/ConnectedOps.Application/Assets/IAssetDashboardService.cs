namespace ConnectedOps.Application.Assets;

public interface IAssetDashboardService
{
    Task<AssetDashboardSummaryDto> GetDashboardSummaryAsync(Guid? branchId = null, CancellationToken cancellationToken = default);
}
