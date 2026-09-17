using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Integrations;

public interface IFuelClearinghouseService
{
    Task<FuelFeedSyncLogDto> TriggerSyncAsync(TriggerFuelSyncRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<FuelFeedSyncLogDto>> GetSyncLogsPagedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<IntegrationsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
