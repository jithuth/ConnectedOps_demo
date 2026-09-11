using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class OrganizationProfileService : IOrganizationProfileService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public OrganizationProfileService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<OrganizationProfileDto> GetProfileAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var profile = await _dbContext.OrganizationProfiles
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (profile is null)
        {
            var tenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
                ?? throw new KeyNotFoundException("Tenant not found.");

            profile = new OrganizationProfile(
                tenantId,
                tenant.Name,
                primaryContactEmail: tenant.Email,
                primaryContactPhone: tenant.Phone,
                countryCode: tenant.CountryCode);

            _dbContext.OrganizationProfiles.Add(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(profile);
    }

    public async Task<OrganizationProfileDto> UpdateProfileAsync(
        UpdateOrganizationProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var profile = await _dbContext.OrganizationProfiles
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (profile is null)
        {
            profile = new OrganizationProfile(
                tenantId,
                request.LegalName,
                request.TradeName,
                request.RegistrationNumber,
                request.TaxNumber,
                request.Website,
                request.PrimaryContactEmail,
                request.PrimaryContactPhone,
                request.AddressLine1,
                request.AddressLine2,
                request.City,
                request.StateOrProvince,
                request.PostalCode,
                request.CountryCode,
                request.CurrencyCode,
                request.TimeZoneId);

            _dbContext.OrganizationProfiles.Add(profile);
        }
        else
        {
            profile.SetLegalName(request.LegalName);
            profile.UpdateDetails(
                request.TradeName,
                request.RegistrationNumber,
                request.TaxNumber,
                request.Website,
                request.PrimaryContactEmail,
                request.PrimaryContactPhone,
                request.AddressLine1,
                request.AddressLine2,
                request.City,
                request.StateOrProvince,
                request.PostalCode,
                request.CountryCode,
                request.CurrencyCode,
                request.TimeZoneId);
        }

        profile.MarkUpdated(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "OrganizationProfile",
                profile.Id.ToString(),
                $"Updated organization profile: {profile.LegalName}"),
            cancellationToken);

        return MapToDto(profile);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static OrganizationProfileDto MapToDto(OrganizationProfile profile) =>
        new(
            profile.Id,
            profile.TenantId,
            profile.LegalName,
            profile.TradeName,
            profile.RegistrationNumber,
            profile.TaxNumber,
            profile.Website,
            profile.PrimaryContactEmail,
            profile.PrimaryContactPhone,
            profile.AddressLine1,
            profile.AddressLine2,
            profile.City,
            profile.StateOrProvince,
            profile.PostalCode,
            profile.CountryCode,
            profile.CurrencyCode,
            profile.TimeZoneId,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
}
