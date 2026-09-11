using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Drivers;

public sealed class DriverCertificationService : IDriverCertificationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public DriverCertificationService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<DriverCertificationDto>> GetCertificationsAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driverExists = await _dbContext.Drivers
            .AnyAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (!driverExists)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        var certs = await _dbContext.DriverCertifications
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.DriverId == driverId)
            .OrderByDescending(c => c.ExpiryDate)
            .ToListAsync(cancellationToken);

        return certs.Select(MapToDto).ToList();
    }

    public async Task<DriverCertificationDto> GetCertificationByIdAsync(
        Guid driverId,
        Guid certificationId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var cert = await _dbContext.DriverCertifications
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == certificationId && c.DriverId == driverId && c.TenantId == tenantId, cancellationToken);

        if (cert is null)
            throw new KeyNotFoundException($"Driver certification '{certificationId}' was not found.");

        return MapToDto(cert);
    }

    public async Task<DriverCertificationDto> AddCertificationAsync(
        Guid driverId,
        CreateDriverCertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (driver is null)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        var cert = new DriverCertification(
            tenantId,
            driverId,
            request.CertificationType,
            request.Title,
            request.CertificateNumber,
            request.IssuedBy,
            request.IssueDate,
            request.ExpiryDate,
            request.FileObjectKey,
            request.Notes);

        _dbContext.DriverCertifications.Add(cert);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "DriverCertification",
                cert.Id.ToString(),
                $"Added certification {cert.Title} for driver {driver.DisplayName}"),
            cancellationToken);

        return MapToDto(cert);
    }

    public async Task<DriverCertificationDto> UpdateCertificationAsync(
        Guid driverId,
        Guid certificationId,
        UpdateDriverCertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var cert = await _dbContext.DriverCertifications
            .FirstOrDefaultAsync(c => c.Id == certificationId && c.DriverId == driverId && c.TenantId == tenantId, cancellationToken);

        if (cert is null)
            throw new KeyNotFoundException($"Driver certification '{certificationId}' was not found.");

        cert.Update(
            request.CertificationType,
            request.Title,
            request.CertificateNumber,
            request.IssuedBy,
            request.IssueDate,
            request.ExpiryDate,
            request.FileObjectKey,
            request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "DriverCertification",
                cert.Id.ToString(),
                $"Updated certification {cert.Title}"),
            cancellationToken);

        return MapToDto(cert);
    }

    public async Task DeactivateCertificationAsync(
        Guid driverId,
        Guid certificationId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var cert = await _dbContext.DriverCertifications
            .FirstOrDefaultAsync(c => c.Id == certificationId && c.DriverId == driverId && c.TenantId == tenantId, cancellationToken);

        if (cert is null)
            throw new KeyNotFoundException($"Driver certification '{certificationId}' was not found.");

        cert.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deactivated,
                "DriverCertification",
                cert.Id.ToString(),
                $"Deactivated certification {cert.Title}"),
            cancellationToken);
    }

    public async Task DeleteCertificationAsync(
        Guid driverId,
        Guid certificationId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var cert = await _dbContext.DriverCertifications
            .FirstOrDefaultAsync(c => c.Id == certificationId && c.DriverId == driverId && c.TenantId == tenantId, cancellationToken);

        if (cert is null)
            throw new KeyNotFoundException($"Driver certification '{certificationId}' was not found.");

        cert.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "DriverCertification",
                cert.Id.ToString(),
                $"Deleted certification {cert.Title}"),
            cancellationToken);
    }

    private static DriverCertificationDto MapToDto(DriverCertification c) =>
        new(
            c.Id,
            c.DriverId,
            c.CertificationType,
            c.CertificationType.ToString(),
            c.Title,
            c.CertificateNumber,
            c.IssuedBy,
            c.IssueDate,
            c.ExpiryDate,
            c.IsActive,
            c.IsExpired(),
            c.IsExpiringSoon(),
            c.FileObjectKey,
            c.Notes);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}
