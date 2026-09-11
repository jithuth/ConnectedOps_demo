using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Assets;

public interface IAssetCustodyService
{
    // Employee Assignment
    Task<AssetEmployeeAssignmentDto> AssignToEmployeeAsync(AssignAssetToEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<AssetEmployeeAssignmentDto> ReturnFromEmployeeAsync(Guid assignmentId, ReturnAssetFromEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetEmployeeAssignmentDto>> GetEmployeeAssignmentsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetEmployeeAssignmentDto>> GetActiveAssignmentsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    // Vehicle Assignment
    Task<AssetVehicleAssignmentDto> AssignToVehicleAsync(AssignAssetToVehicleRequest request, CancellationToken cancellationToken = default);
    Task<AssetVehicleAssignmentDto> RemoveFromVehicleAsync(Guid assignmentId, RemoveAssetFromVehicleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetVehicleAssignmentDto>> GetVehicleAssignmentsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetVehicleAssignmentDto>> GetActiveAssignmentsByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    // Usage Sessions (Check-out / Check-in)
    Task<AssetUsageSessionDto> CheckOutAsync(StartAssetUsageSessionRequest request, CancellationToken cancellationToken = default);
    Task<AssetUsageSessionDto> CheckInAsync(Guid sessionId, EndAssetUsageSessionRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<AssetUsageSessionDto>> GetUsageSessionsPagedAsync(Guid? assetId = null, Guid? employeeId = null, Guid? vehicleId = null, bool? activeOnly = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<AssetUsageSessionDto?> GetActiveSessionByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
}
