namespace ConnectedOps.Application.Assets;

public interface IAssetCategoryService
{
    Task<IReadOnlyList<AssetCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetCategoryTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<AssetCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetCategoryDto> CreateAsync(CreateAssetCategoryRequest request, CancellationToken cancellationToken = default);
    Task<AssetCategoryDto> UpdateAsync(Guid id, UpdateAssetCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
