namespace ConnectedOps.Application.Assets;

public sealed record AssetTypeDto(
    Guid Id,
    Guid CategoryId,
    string? CategoryName,
    string Code,
    string Name,
    string? Description,
    bool RequiresInspection,
    int? DefaultInspectionIntervalDays,
    bool RequiresCalibration,
    int? DefaultCalibrationIntervalDays,
    bool TrackUsageHours,
    bool IsActive,
    int AssetCount,
    DateTime CreatedAtUtc);

public sealed record CreateAssetTypeRequest(
    Guid CategoryId,
    string Code,
    string Name,
    string? Description,
    bool RequiresInspection = false,
    int? DefaultInspectionIntervalDays = null,
    bool RequiresCalibration = false,
    int? DefaultCalibrationIntervalDays = null,
    bool TrackUsageHours = false,
    bool IsActive = true);

public sealed record UpdateAssetTypeRequest(
    Guid CategoryId,
    string Code,
    string Name,
    string? Description,
    bool RequiresInspection,
    int? DefaultInspectionIntervalDays,
    bool RequiresCalibration,
    int? DefaultCalibrationIntervalDays,
    bool TrackUsageHours,
    bool IsActive);
