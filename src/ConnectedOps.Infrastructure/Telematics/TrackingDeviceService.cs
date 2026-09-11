using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class TrackingDeviceService : ITrackingDeviceService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDeviceConnectivityService _connectivityService;
    private readonly IAuditLogService _auditLogService;

    public TrackingDeviceService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDeviceConnectivityService connectivityService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _connectivityService = connectivityService;
        _auditLogService = auditLogService;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId ??
               throw new InvalidOperationException("Active tenant context is required for tracking device operations.");
    }

    public async Task<IReadOnlyCollection<TrackingDeviceListItemDto>> GetDevicesPagedAsync(
        TrackingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = BuildQuery(parameters, tenantId);

        var devices = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(x => new
            {
                Device = x,
                ProviderCode = x.Provider.Code,
                DeviceTypeName = x.DeviceType.Name,
                ActiveAssignment = _dbContext.TrackingDeviceVehicleAssignments
                    .Where(a => a.TrackingDeviceId == x.Id && a.IsActive)
                    .Select(a => new { a.VehicleId, a.Vehicle.DisplayName, a.Vehicle.VehicleNumber })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var settings = await _dbContext.TelematicsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        int threshold = settings?.OfflineThresholdMinutes ?? 5;

        return devices.Select(d => new TrackingDeviceListItemDto(
            d.Device.Id,
            d.Device.DeviceIdentifier,
            d.Device.IMEI,
            d.Device.Name,
            d.ProviderCode,
            d.DeviceTypeName,
            d.Device.Status,
            _connectivityService.EvaluateStatus(d.Device.LastSeenAtUtc, threshold),
            d.Device.LastSeenAtUtc,
            d.ActiveAssignment?.VehicleId,
            d.ActiveAssignment?.DisplayName ?? d.ActiveAssignment?.VehicleNumber,
            d.Device.BatteryLevelPercent,
            d.Device.SignalStrength,
            d.Device.IsActive)).ToList();
    }

    public async Task<int> GetDeviceCountAsync(
        TrackingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = BuildQuery(parameters, tenantId);
        return await query.CountAsync(cancellationToken);
    }

    private IQueryable<TrackingDevice> BuildQuery(TrackingDeviceQueryParameters parameters, Guid tenantId)
    {
        var query = _dbContext.TrackingDevices
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.DeviceType)
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(x =>
                x.DeviceIdentifier.ToLower().Contains(search) ||
                (x.IMEI != null && x.IMEI.ToLower().Contains(search)) ||
                (x.SerialNumber != null && x.SerialNumber.ToLower().Contains(search)) ||
                (x.Name != null && x.Name.ToLower().Contains(search)) ||
                (x.Model != null && x.Model.ToLower().Contains(search)));
        }

        if (parameters.ProviderId.HasValue)
        {
            query = query.Where(x => x.ProviderId == parameters.ProviderId.Value);
        }

        if (parameters.DeviceTypeId.HasValue)
        {
            query = query.Where(x => x.DeviceTypeId == parameters.DeviceTypeId.Value);
        }

        if (parameters.Status.HasValue)
        {
            query = query.Where(x => x.Status == parameters.Status.Value);
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == parameters.IsActive.Value);
        }

        if (parameters.IsAssigned.HasValue)
        {
            var assignedDeviceIds = _dbContext.TrackingDeviceVehicleAssignments
                .Where(a => a.TenantId == tenantId && a.IsActive)
                .Select(a => a.TrackingDeviceId);

            if (parameters.IsAssigned.Value)
            {
                query = query.Where(x => assignedDeviceIds.Contains(x.Id));
            }
            else
            {
                query = query.Where(x => !assignedDeviceIds.Contains(x.Id));
            }
        }

        return query;
    }

    public async Task<TrackingDeviceDto?> GetDeviceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var device = await _dbContext.TrackingDevices
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.DeviceType)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (device == null)
            return null;

        var activeAssignment = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.TrackingDeviceId == device.Id && a.IsActive, cancellationToken);

        return MapToDto(device, activeAssignment);
    }

    public async Task<TrackingDeviceDto?> GetDeviceByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var normalized = identifier.Trim();

        var device = await _dbContext.TrackingDevices
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.DeviceType)
            .FirstOrDefaultAsync(x => x.DeviceIdentifier == normalized && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (device == null)
            return null;

        var activeAssignment = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.TrackingDeviceId == device.Id && a.IsActive, cancellationToken);

        return MapToDto(device, activeAssignment);
    }

    public async Task<TrackingDeviceDto?> GetDeviceByImeiAsync(
        string imei,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var normalized = imei.Trim();

        var device = await _dbContext.TrackingDevices
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.DeviceType)
            .FirstOrDefaultAsync(x => x.IMEI == normalized && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (device == null)
            return null;

        var activeAssignment = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.TrackingDeviceId == device.Id && a.IsActive, cancellationToken);

        return MapToDto(device, activeAssignment);
    }

    public async Task<TrackingDeviceDto> CreateDeviceAsync(
        CreateTrackingDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var normalizedIdentifier = request.DeviceIdentifier.Trim();

        // 1. Check identifier uniqueness within tenant
        var existsId = await _dbContext.TrackingDevices
            .AnyAsync(x => x.TenantId == tenantId && x.DeviceIdentifier == normalizedIdentifier && !x.IsDeleted, cancellationToken);

        if (existsId)
        {
            throw new InvalidOperationException($"A tracking device with identifier '{normalizedIdentifier}' already exists.");
        }

        // 2. Check IMEI uniqueness within tenant if provided
        if (!string.IsNullOrWhiteSpace(request.IMEI))
        {
            var normalizedImei = request.IMEI.Trim();
            var existsImei = await _dbContext.TrackingDevices
                .AnyAsync(x => x.TenantId == tenantId && x.IMEI == normalizedImei && !x.IsDeleted, cancellationToken);

            if (existsImei)
            {
                throw new InvalidOperationException($"A tracking device with IMEI '{normalizedImei}' already exists.");
            }
        }

        // 3. Verify provider and device type exist
        var provider = await _dbContext.TrackingProviders
            .FirstOrDefaultAsync(x => x.Id == request.ProviderId && (x.TenantId == null || x.TenantId == tenantId), cancellationToken)
            ?? throw new InvalidOperationException("Selected provider was not found.");

        var deviceType = await _dbContext.TrackingDeviceTypes
            .FirstOrDefaultAsync(x => x.Id == request.DeviceTypeId && (x.TenantId == null || x.TenantId == tenantId), cancellationToken)
            ?? throw new InvalidOperationException("Selected device type was not found.");

        var device = new TrackingDevice(
            tenantId,
            normalizedIdentifier,
            provider.Id,
            deviceType.Id,
            request.IMEI,
            request.SerialNumber,
            request.Name,
            request.Model,
            request.Manufacturer,
            request.FirmwareVersion,
            request.SIMNumber,
            request.SIMICCID,
            request.PhoneNumber);

        _dbContext.TrackingDevices.Add(device);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.TrackingDeviceCreated,
                "TrackingDevice",
                device.Id.ToString(),
                $"Registered tracking device '{device.DeviceIdentifier}' ({provider.Name} - {deviceType.Name})."),
            cancellationToken);

        return MapToDto(device, null, provider.Name, provider.Code, deviceType.Name, deviceType.Code);
    }

    public async Task<TrackingDeviceDto> UpdateDeviceAsync(
        Guid id,
        UpdateTrackingDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var device = await _dbContext.TrackingDevices
            .Include(x => x.Provider)
            .Include(x => x.DeviceType)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Tracking device not found.");

        if (!string.IsNullOrWhiteSpace(request.IMEI))
        {
            var normalizedImei = request.IMEI.Trim();
            var existsImei = await _dbContext.TrackingDevices
                .AnyAsync(x => x.TenantId == tenantId && x.IMEI == normalizedImei && x.Id != id && !x.IsDeleted, cancellationToken);

            if (existsImei)
            {
                throw new InvalidOperationException($"Another tracking device with IMEI '{normalizedImei}' already exists.");
            }
        }

        device.Update(
            request.Name,
            request.ProviderId,
            request.DeviceTypeId,
            request.IMEI,
            request.SerialNumber,
            request.Model,
            request.Manufacturer,
            request.FirmwareVersion,
            request.SIMNumber,
            request.SIMICCID,
            request.PhoneNumber,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.TrackingDeviceUpdated,
                "TrackingDevice",
                device.Id.ToString(),
                $"Updated tracking device '{device.DeviceIdentifier}'."),
            cancellationToken);

        var activeAssignment = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.TrackingDeviceId == device.Id && a.IsActive, cancellationToken);

        return MapToDto(device, activeAssignment);
    }

    public async Task<bool> ActivateDeviceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var device = await _dbContext.TrackingDevices
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (device == null)
            return false;

        device.Activate(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.TrackingDeviceActivated,
                "TrackingDevice",
                device.Id.ToString(),
                $"Activated tracking device '{device.DeviceIdentifier}'."),
            cancellationToken);

        return true;
    }

    public async Task<bool> DeactivateDeviceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var device = await _dbContext.TrackingDevices
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (device == null)
            return false;

        device.Deactivate(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.TrackingDeviceDeactivated,
                "TrackingDevice",
                device.Id.ToString(),
                $"Deactivated tracking device '{device.DeviceIdentifier}'."),
            cancellationToken);

        return true;
    }

    public async Task<bool> DeleteDeviceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var device = await _dbContext.TrackingDevices
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (device == null)
            return false;

        // End any active vehicle assignment
        var activeAssignment = await _dbContext.TrackingDeviceVehicleAssignments
            .FirstOrDefaultAsync(a => a.TrackingDeviceId == device.Id && a.IsActive, cancellationToken);

        if (activeAssignment != null)
        {
            activeAssignment.EndAssignment(_currentUserContext.UserId, DateTime.UtcNow, "Device deleted.");
        }

        device.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "TrackingDevice",
                device.Id.ToString(),
                $"Deleted tracking device '{device.DeviceIdentifier}'."),
            cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<TrackingDeviceListItemDto>> GetAvailableDevicesForAssignmentAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var assignedDeviceIds = await _dbContext.TrackingDeviceVehicleAssignments
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .Select(a => a.TrackingDeviceId)
            .ToListAsync(cancellationToken);

        var devices = await _dbContext.TrackingDevices
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.DeviceType)
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted && !assignedDeviceIds.Contains(x.Id))
            .OrderBy(x => x.DeviceIdentifier)
            .Select(x => new TrackingDeviceListItemDto(
                x.Id,
                x.DeviceIdentifier,
                x.IMEI,
                x.Name,
                x.Provider.Code,
                x.DeviceType.Name,
                x.Status,
                DeviceConnectivityStatus.Unknown,
                x.LastSeenAtUtc,
                null,
                null,
                x.BatteryLevelPercent,
                x.SignalStrength,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return devices;
    }

    private static TrackingDeviceDto MapToDto(
        TrackingDevice device,
        TrackingDeviceVehicleAssignment? activeAssignment,
        string? providerName = null,
        string? providerCode = null,
        string? deviceTypeName = null,
        string? deviceTypeCode = null)
    {
        return new TrackingDeviceDto(
            device.Id,
            device.TenantId,
            device.DeviceIdentifier,
            device.IMEI,
            device.SerialNumber,
            device.Name,
            device.ProviderId,
            providerName ?? device.Provider?.Name ?? "Unknown",
            providerCode ?? device.Provider?.Code ?? "UNKNOWN",
            device.DeviceTypeId,
            deviceTypeName ?? device.DeviceType?.Name ?? "Unknown",
            deviceTypeCode ?? device.DeviceType?.Code ?? "UNKNOWN",
            device.Model,
            device.Manufacturer,
            device.FirmwareVersion,
            device.SIMNumber,
            device.SIMICCID,
            device.PhoneNumber,
            device.Status,
            device.LastSeenAtUtc,
            device.LastTelemetryAtUtc,
            device.LastKnownLatitude,
            device.LastKnownLongitude,
            device.LastKnownSpeedKph,
            device.LastKnownHeadingDegrees,
            device.LastKnownIgnition,
            device.BatteryLevelPercent,
            device.ExternalPowerVoltage,
            device.BatteryVoltage,
            device.SignalStrength,
            device.IsActive,
            activeAssignment?.VehicleId,
            activeAssignment?.Vehicle?.VehicleNumber,
            activeAssignment?.Vehicle?.DisplayName ?? activeAssignment?.Vehicle?.VehicleNumber,
            device.CreatedAtUtc,
            device.UpdatedAtUtc);
    }
}
