namespace ConnectedOps.Application.Assets;

public interface IAssetIdentifierService
{
    Task<IReadOnlyList<AssetIdentifierDto>> GetIdentifiersByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<AssetIdentifierDto> CreateIdentifierAsync(CreateAssetIdentifierRequest request, CancellationToken cancellationToken = default);
    Task<AssetIdentifierDto> GenerateQrTokenAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<AssetScanResultDto?> ScanByTokenAsync(string publicToken, CancellationToken cancellationToken = default);
    Task DeactivateIdentifierAsync(Guid identifierId, CancellationToken cancellationToken = default);
}
