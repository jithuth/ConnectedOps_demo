using ConnectedOps.Domain.Telematics;

namespace ConnectedOps.Application.Telematics;

public sealed record DeviceCommandDto(
    Guid Id,
    Guid TenantId,
    Guid TrackingDeviceId,
    string DeviceIdentifier,
    DeviceCommandType CommandType,
    string? ParametersJson,
    DeviceCommandStatus Status,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc,
    DateTime? AcknowledgedAtUtc,
    DateTime? FailedAtUtc,
    string? ResponsePayload,
    string? FailureReason,
    Guid? CreatedByUserId);

public sealed record CreateDeviceCommandRequest(
    DeviceCommandType CommandType,
    string? ParametersJson = null);

public sealed record CancelDeviceCommandRequest(
    string? Reason = null);
