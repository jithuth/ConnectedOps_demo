using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Drivers;

public sealed class DriverLicenseService : IDriverLicenseService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public DriverLicenseService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<DriverLicenseDto>> GetLicensesAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driverExists = await _dbContext.Drivers
            .AnyAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (!driverExists)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        var licenses = await _dbContext.DriverLicenses
            .AsNoTracking()
            .Include(l => l.Categories)
            .Where(l => l.TenantId == tenantId && l.DriverId == driverId)
            .OrderByDescending(l => l.IsPrimary)
            .ThenByDescending(l => l.ExpiryDate)
            .ToListAsync(cancellationToken);

        return licenses.Select(MapToDto).ToList();
    }

    public async Task<DriverLicenseDto> GetLicenseByIdAsync(
        Guid driverId,
        Guid licenseId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var license = await _dbContext.DriverLicenses
            .AsNoTracking()
            .Include(l => l.Categories)
            .FirstOrDefaultAsync(l => l.Id == licenseId && l.DriverId == driverId && l.TenantId == tenantId, cancellationToken);

        if (license is null)
            throw new KeyNotFoundException($"Driver license '{licenseId}' was not found.");

        return MapToDto(license);
    }

    public async Task<DriverLicenseDto> AddLicenseAsync(
        Guid driverId,
        CreateDriverLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .Include(d => d.Licenses)
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        // If marked primary or it's the driver's first license, make sure other licenses are not primary
        var isPrimary = request.IsPrimary || !driver.Licenses.Any(l => l.IsActive);
        if (isPrimary)
        {
            var existingLicenses = await _dbContext.DriverLicenses
                .Where(l => l.TenantId == tenantId && l.DriverId == driverId && l.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingLicenses)
            {
                existing.SetPrimary(false);
            }
        }

        var license = new DriverLicense(
            tenantId,
            driverId,
            request.LicenseNumber,
            request.LicenseCountryCode,
            request.IssuingAuthority,
            request.IssueDate,
            request.ExpiryDate,
            isPrimary,
            request.Notes);

        if (request.CategoryCodes != null)
        {
            foreach (var code in request.CategoryCodes.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                var cat = new DriverLicenseCategory(tenantId, license.Id, code);
                _dbContext.DriverLicenseCategories.Add(cat);
            }
        }

        _dbContext.DriverLicenses.Add(license);

        if (isPrimary)
        {
            driver.SetPrimaryLicenseNumber(license.LicenseNumber);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "DriverLicense",
                license.Id.ToString(),
                $"Added license {license.LicenseNumber} for driver {driver.DisplayName}"),
            cancellationToken);

        return await GetLicenseByIdAsync(driverId, license.Id, cancellationToken);
    }

    public async Task<DriverLicenseDto> UpdateLicenseAsync(
        Guid driverId,
        Guid licenseId,
        UpdateDriverLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var license = await _dbContext.DriverLicenses
            .Include(l => l.Driver)
            .FirstOrDefaultAsync(l => l.Id == licenseId && l.DriverId == driverId && l.TenantId == tenantId, cancellationToken);

        if (license is null)
            throw new KeyNotFoundException($"Driver license '{licenseId}' was not found.");

        license.Update(
            request.LicenseNumber,
            request.LicenseCountryCode,
            request.IssuingAuthority,
            request.IssueDate,
            request.ExpiryDate,
            request.Notes);

        if (license.IsPrimary && license.Driver != null)
        {
            license.Driver.SetPrimaryLicenseNumber(license.LicenseNumber);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "DriverLicense",
                license.Id.ToString(),
                $"Updated license {license.LicenseNumber}"),
            cancellationToken);

        return await GetLicenseByIdAsync(driverId, licenseId, cancellationToken);
    }

    public async Task<DriverLicenseDto> SetPrimaryLicenseAsync(
        Guid driverId,
        Guid licenseId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (driver is null)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        var licenses = await _dbContext.DriverLicenses
            .Where(l => l.DriverId == driverId && l.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var targetLicense = licenses.FirstOrDefault(l => l.Id == licenseId);
        if (targetLicense is null)
            throw new KeyNotFoundException($"Driver license '{licenseId}' was not found.");

        foreach (var l in licenses)
        {
            l.SetPrimary(l.Id == licenseId);
        }

        targetLicense.Activate();
        driver.SetPrimaryLicenseNumber(targetLicense.LicenseNumber);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "DriverLicense",
                targetLicense.Id.ToString(),
                $"Marked license {targetLicense.LicenseNumber} as primary for driver {driver.DisplayName}"),
            cancellationToken);

        return await GetLicenseByIdAsync(driverId, licenseId, cancellationToken);
    }

    public async Task DeactivateLicenseAsync(
        Guid driverId,
        Guid licenseId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var license = await _dbContext.DriverLicenses
            .FirstOrDefaultAsync(l => l.Id == licenseId && l.DriverId == driverId && l.TenantId == tenantId, cancellationToken);

        if (license is null)
            throw new KeyNotFoundException($"Driver license '{licenseId}' was not found.");

        license.Deactivate();

        // If it was primary, clear or find another active license for driver
        if (license.IsPrimary)
        {
            var driver = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
            if (driver != null)
            {
                var nextActive = await _dbContext.DriverLicenses
                    .FirstOrDefaultAsync(l => l.DriverId == driverId && l.Id != licenseId && l.IsActive && l.TenantId == tenantId, cancellationToken);

                if (nextActive != null)
                {
                    nextActive.SetPrimary(true);
                    driver.SetPrimaryLicenseNumber(nextActive.LicenseNumber);
                }
                else
                {
                    driver.SetPrimaryLicenseNumber(null);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deactivated,
                "DriverLicense",
                license.Id.ToString(),
                $"Deactivated license {license.LicenseNumber}"),
            cancellationToken);
    }

    public async Task<DriverLicenseCategoryDto> AddCategoryAsync(
        Guid driverId,
        Guid licenseId,
        AddDriverLicenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var licenseExists = await _dbContext.DriverLicenses
            .AnyAsync(l => l.Id == licenseId && l.DriverId == driverId && l.TenantId == tenantId, cancellationToken);
        if (!licenseExists)
            throw new KeyNotFoundException($"Driver license '{licenseId}' was not found.");

        var normalizedCode = request.CategoryCode.Trim().ToUpperInvariant();
        var exists = await _dbContext.DriverLicenseCategories
            .AnyAsync(c => c.TenantId == tenantId && c.DriverLicenseId == licenseId && c.CategoryCode == normalizedCode, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Category code '{normalizedCode}' already exists for this license.");

        var category = new DriverLicenseCategory(
            tenantId,
            licenseId,
            normalizedCode,
            request.Description,
            request.ValidFrom,
            request.ValidTo);

        _dbContext.DriverLicenseCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DriverLicenseCategoryDto(
            category.Id,
            category.DriverLicenseId,
            category.CategoryCode,
            category.Description,
            category.ValidFrom,
            category.ValidTo,
            category.IsActive);
    }

    public async Task<DriverLicenseCategoryDto> UpdateCategoryAsync(
        Guid driverId,
        Guid licenseId,
        Guid categoryId,
        UpdateDriverLicenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var category = await _dbContext.DriverLicenseCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.DriverLicenseId == licenseId && c.TenantId == tenantId, cancellationToken);

        if (category is null)
            throw new KeyNotFoundException($"License category '{categoryId}' was not found.");

        category.Update(request.Description, request.ValidFrom, request.ValidTo);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DriverLicenseCategoryDto(
            category.Id,
            category.DriverLicenseId,
            category.CategoryCode,
            category.Description,
            category.ValidFrom,
            category.ValidTo,
            category.IsActive);
    }

    public async Task DeleteCategoryAsync(
        Guid driverId,
        Guid licenseId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var category = await _dbContext.DriverLicenseCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.DriverLicenseId == licenseId && c.TenantId == tenantId, cancellationToken);

        if (category is null)
            throw new KeyNotFoundException($"License category '{categoryId}' was not found.");

        category.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DriverLicenseDto MapToDto(DriverLicense l) =>
        new(
            l.Id,
            l.DriverId,
            l.LicenseNumber,
            l.LicenseCountryCode,
            l.IssuingAuthority,
            l.IssueDate,
            l.ExpiryDate,
            l.IsPrimary,
            l.IsActive,
            l.IsExpired(),
            l.IsExpiringSoon(),
            l.Notes,
            l.Categories.Select(c => new DriverLicenseCategoryDto(
                c.Id,
                c.DriverLicenseId,
                c.CategoryCode,
                c.Description,
                c.ValidFrom,
                c.ValidTo,
                c.IsActive)).ToList());

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}
