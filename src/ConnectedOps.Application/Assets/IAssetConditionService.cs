namespace ConnectedOps.Application.Assets;

public interface IAssetConditionService
{
    Task<IReadOnlyList<AssetConditionRecordDto>> GetConditionHistoryByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<AssetConditionRecordDto> RecordConditionAsync(CreateAssetConditionRecordRequest request, CancellationToken cancellationToken = default);
}
