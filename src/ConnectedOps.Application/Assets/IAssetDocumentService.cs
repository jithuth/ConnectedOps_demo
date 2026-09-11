namespace ConnectedOps.Application.Assets;

public interface IAssetDocumentService
{
    Task<IReadOnlyList<AssetDocumentDto>> GetDocumentsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<AssetDocumentDto> AddDocumentAsync(CreateAssetDocumentRequest request, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
