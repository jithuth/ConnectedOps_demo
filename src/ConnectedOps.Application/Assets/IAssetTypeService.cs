namespace ConnectedOps.Application.Assets;

public interface IAssetTypeService
{
    Task<IReadOnlyList<AssetTypeDto>> GetAllAsync(Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<AssetTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetTypeDto> CreateAsync(CreateAssetTypeRequest request, CancellationToken cancellationToken = default);
    Task<AssetTypeDto> UpdateAsync(Guid id, UpdateAssetTypeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
