using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Assets;

namespace ConnectedOps.Application.Assets;

public interface IAssetService
{
    Task<PagedResult<AssetDto>> GetPagedAsync(AssetListFilter filter, CancellationToken cancellationToken = default);
    Task<AssetDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto> UpdateAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetDto> ChangeStatusAsync(Guid id, ChangeAssetStatusRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto> UpdateLocationAsync(Guid id, UpdateAssetLocationRequest request, CancellationToken cancellationToken = default);
    Task<AssetNoteDto> AddNoteAsync(Guid id, AddAssetNoteRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetNoteDto>> GetNotesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetLocationHistoryDto>> GetLocationHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
