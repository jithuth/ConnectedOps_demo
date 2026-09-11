using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Assets;

public interface IAssetInspectionService
{
    // Inspections
    Task<PagedResult<AssetInspectionDto>> GetInspectionsPagedAsync(AssetInspectionFilter filter, CancellationToken cancellationToken = default);
    Task<AssetInspectionDto?> GetInspectionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetInspectionDto> CreateInspectionAsync(CreateAssetInspectionRequest request, CancellationToken cancellationToken = default);
    Task<AssetInspectionDto> CompleteInspectionAsync(Guid id, CompleteAssetInspectionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetInspectionDto>> GetInspectionsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    // Calibrations
    Task<IReadOnlyList<AssetCalibrationRecordDto>> GetCalibrationsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<AssetCalibrationRecordDto> AddCalibrationRecordAsync(CreateAssetCalibrationRecordRequest request, CancellationToken cancellationToken = default);
}
