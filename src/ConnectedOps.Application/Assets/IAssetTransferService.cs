using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Assets;

public interface IAssetTransferService
{
    Task<PagedResult<AssetTransferDto>> GetPagedAsync(AssetTransferFilter filter, CancellationToken cancellationToken = default);
    Task<AssetTransferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetTransferDto> CreateTransferAsync(CreateAssetTransferRequest request, CancellationToken cancellationToken = default);
    Task<AssetTransferDto> CompleteTransferAsync(Guid id, CompleteAssetTransferRequest request, CancellationToken cancellationToken = default);
    Task<AssetTransferDto> CancelTransferAsync(Guid id, CancelAssetTransferRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetTransferDto>> GetTransfersByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
}
