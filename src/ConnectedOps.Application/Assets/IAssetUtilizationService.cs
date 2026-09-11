namespace ConnectedOps.Application.Assets;

public interface IAssetUtilizationService
{
    Task<AssetUtilizationSummaryDto> GetUtilizationSummaryAsync(Guid? branchId = null, Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetIdleReportDto>> GetIdleAssetsAsync(int idleDaysThreshold = 30, Guid? branchId = null, CancellationToken cancellationToken = default);
}
