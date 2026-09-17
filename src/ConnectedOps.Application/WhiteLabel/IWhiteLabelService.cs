using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.WhiteLabel;

public interface IWhiteLabelService
{
    Task<WhiteLabelOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<TenantBrandingDto> GetBrandingAsync(CancellationToken cancellationToken = default);

    Task<TenantBrandingDto> UpdateBrandingAsync(
        UpdateBrandingRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantCustomDomainDto> RegisterCustomDomainAsync(
        RegisterCustomDomainRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantCustomDomainDto> VerifyCustomDomainAsync(
        VerifyCustomDomainRequest request,
        CancellationToken cancellationToken = default);

    Task<AuditCompliancePackageDto> GenerateAuditPackageAsync(
        GenerateAuditPackageRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditCompliancePackageDto>> GetAuditPackagesPagedAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
