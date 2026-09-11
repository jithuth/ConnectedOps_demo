using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class DeviceCommandService : IDeviceCommandService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public DeviceCommandService(
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
               throw new InvalidOperationException("Active tenant context is required for device commands.");
    }

    public async Task<DeviceCommandDto> SendCommandAsync(
        Guid deviceId,
        CreateDeviceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var device = await _dbContext.TrackingDevices
            .FirstOrDefaultAsync(x => x.Id == deviceId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Tracking device not found.");

        if (!device.IsActive)
        {
            throw new InvalidOperationException("Cannot send commands to an inactive device.");
        }

        var command = new DeviceCommand(
            tenantId,
            device.Id,
            request.CommandType,
            request.ParametersJson,
            _currentUserContext.UserId);

        // Transition to Sent for safe diagnostic commands
        command.MarkSent(_currentUserContext.UserId);

        _dbContext.DeviceCommands.Add(command);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.DeviceCommandCreated,
                "DeviceCommand",
                command.Id.ToString(),
                $"Issued command {command.CommandType} to device '{device.DeviceIdentifier}'."),
            cancellationToken);

        return new DeviceCommandDto(
            command.Id,
            command.TenantId,
            command.TrackingDeviceId,
            device.DeviceIdentifier,
            command.CommandType,
            command.ParametersJson,
            command.Status,
            command.CreatedAtUtc,
            command.SentAtUtc,
            command.AcknowledgedAtUtc,
            command.FailedAtUtc,
            command.ResponsePayload,
            command.FailureReason,
            command.CreatedBy);
    }

    public async Task<DeviceCommandDto> CancelCommandAsync(
        Guid commandId,
        CancelDeviceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var command = await _dbContext.DeviceCommands
            .Include(x => x.TrackingDevice)
            .FirstOrDefaultAsync(x => x.Id == commandId && x.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Device command not found.");

        command.Cancel(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.DeviceCommandCancelled,
                "DeviceCommand",
                command.Id.ToString(),
                $"Cancelled command {command.CommandType} for device '{command.TrackingDevice.DeviceIdentifier}'."),
            cancellationToken);

        return new DeviceCommandDto(
            command.Id,
            command.TenantId,
            command.TrackingDeviceId,
            command.TrackingDevice.DeviceIdentifier,
            command.CommandType,
            command.ParametersJson,
            command.Status,
            command.CreatedAtUtc,
            command.SentAtUtc,
            command.AcknowledgedAtUtc,
            command.FailedAtUtc,
            command.ResponsePayload,
            command.FailureReason,
            command.CreatedBy);
    }

    public async Task<DeviceCommandDto?> GetCommandByIdAsync(
        Guid commandId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var command = await _dbContext.DeviceCommands
            .AsNoTracking()
            .Include(x => x.TrackingDevice)
            .FirstOrDefaultAsync(x => x.Id == commandId && x.TenantId == tenantId, cancellationToken);

        if (command == null)
            return null;

        return new DeviceCommandDto(
            command.Id,
            command.TenantId,
            command.TrackingDeviceId,
            command.TrackingDevice.DeviceIdentifier,
            command.CommandType,
            command.ParametersJson,
            command.Status,
            command.CreatedAtUtc,
            command.SentAtUtc,
            command.AcknowledgedAtUtc,
            command.FailedAtUtc,
            command.ResponsePayload,
            command.FailureReason,
            command.CreatedBy);
    }

    public async Task<IReadOnlyCollection<DeviceCommandDto>> GetCommandsForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var commands = await _dbContext.DeviceCommands
            .AsNoTracking()
            .Include(x => x.TrackingDevice)
            .Where(x => x.TrackingDeviceId == deviceId && x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .Select(x => new DeviceCommandDto(
                x.Id,
                x.TenantId,
                x.TrackingDeviceId,
                x.TrackingDevice.DeviceIdentifier,
                x.CommandType,
                x.ParametersJson,
                x.Status,
                x.CreatedAtUtc,
                x.SentAtUtc,
                x.AcknowledgedAtUtc,
                x.FailedAtUtc,
                x.ResponsePayload,
                x.FailureReason,
                x.CreatedBy))
            .ToListAsync(cancellationToken);

        return commands;
    }
}
