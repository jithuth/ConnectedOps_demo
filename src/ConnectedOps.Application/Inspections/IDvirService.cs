using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Inspections;

public interface IDvirService
{
    Task<DvirDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<DvirInspectionDto>> GetInspectionsPagedAsync(
        DvirFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<DvirInspectionDto?> GetInspectionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DvirInspectionDto> CreateInspectionAsync(
        CreateDvirRequest request,
        CancellationToken cancellationToken = default);

    Task<DvirInspectionDto> CertifyByMechanicAsync(
        Guid id,
        SignOffDvirRequest request,
        CancellationToken cancellationToken = default);
}
