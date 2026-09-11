using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class DeviceProvisioningService : IDeviceProvisioningService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public DeviceProvisioningService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId ??
               throw new InvalidOperationException("Active tenant context is required for provisioning operations.");
    }

    public async Task<DeviceProvisioningRecordDto> ProvisionDeviceAsync(
        Guid deviceId,
        ProvisionDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var device = await _dbContext.TrackingDevices
            .FirstOrDefaultAsync(x => x.Id == deviceId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Tracking device not found.");

        device.ChangeStatus(TrackingDeviceStatus.Provisioned, _currentUserContext.UserId);

        var record = new DeviceProvisioningRecord(
            tenantId,
            device.Id,
            ProvisioningStatus.Provisioned,
            _currentUserContext.UserId,
            DateTime.UtcNow,
            request.ProviderReference,
            request.Notes);

        _dbContext.DeviceProvisioningRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.TrackingDeviceProvisioned,
                "TrackingDevice",
                device.Id.ToString(),
                $"Provisioned tracking device '{device.DeviceIdentifier}' (Ref: {request.ProviderReference ?? "N/A"})."),
            cancellationToken);

        return new DeviceProvisioningRecordDto(
            record.Id,
            record.TrackingDeviceId,
            device.DeviceIdentifier,
            record.ProvisioningStatus,
            record.ProvisionedAtUtc,
            record.ProvisionedByUserId,
            record.ProviderReference,
            record.Notes,
            record.CreatedAtUtc);
    }

    public async Task<DeviceProvisioningRecordDto> DeprovisionDeviceAsync(
        Guid deviceId,
        DeprovisionDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var device = await _dbContext.TrackingDevices
            .FirstOrDefaultAsync(x => x.Id == deviceId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Tracking device not found.");

        device.ChangeStatus(TrackingDeviceStatus.Retired, _currentUserContext.UserId);

        var record = new DeviceProvisioningRecord(
            tenantId,
            device.Id,
            ProvisioningStatus.Deprovisioned,
            _currentUserContext.UserId,
            DateTime.UtcNow,
            null,
            request.Notes);

        _dbContext.DeviceProvisioningRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.TrackingDeviceDeprovisioned,
                "TrackingDevice",
                device.Id.ToString(),
                $"Deprovisioned tracking device '{device.DeviceIdentifier}'."),
            cancellationToken);

        return new DeviceProvisioningRecordDto(
            record.Id,
            record.TrackingDeviceId,
            device.DeviceIdentifier,
            record.ProvisioningStatus,
            record.ProvisionedAtUtc,
            record.ProvisionedByUserId,
            record.ProviderReference,
            record.Notes,
            record.CreatedAtUtc);
    }

    public async Task<IReadOnlyCollection<DeviceProvisioningRecordDto>> GetProvisioningHistoryAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var records = await _dbContext.DeviceProvisioningRecords
            .AsNoTracking()
            .Include(x => x.TrackingDevice)
            .Where(x => x.TrackingDeviceId == deviceId && x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DeviceProvisioningRecordDto(
                x.Id,
                x.TrackingDeviceId,
                x.TrackingDevice.DeviceIdentifier,
                x.ProvisioningStatus,
                x.ProvisionedAtUtc,
                x.ProvisionedByUserId,
                x.ProviderReference,
                x.Notes,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return records;
    }
}
