namespace ConnectedOps.Application.Assets;

public interface IAssetActivityTimelineService
{
    Task<AssetActivityTimelineDto> GetTimelineByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
}
