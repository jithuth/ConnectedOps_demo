namespace ConnectedOps.Application.Tracking;

public sealed record PublicTrackingInfoDto(
    string TrackingToken,
    Guid JobId,
    string JobTitle,
    string CustomerName,
    string DeliveryAddress,
    double DestinationLatitude,
    double DestinationLongitude,
    string JobStatus,
    string? VehiclePlateNumber,
    string? VehicleMakeModel,
    double? CurrentVehicleLatitude,
    double? CurrentVehicleLongitude,
    double? CurrentVehicleSpeedKmh,
    string? DriverFirstName,
    DateTime? EstimatedArrivalUtc,
    int? EstimatedMinutesRemaining,
    int? Rating,
    string? FeedbackComment,
    bool IsDelivered);

public sealed record GenerateTrackingTokenRequest(
    Guid DispatchJobId,
    int ExpiryHours = 48);

public sealed record SubmitDeliveryRatingRequest(
    int Rating,
    string? FeedbackComment = null);

public sealed record PublicTrackingTokenDto(
    Guid Id,
    Guid DispatchJobId,
    string Token,
    string CustomerName,
    string CustomerPhone,
    DateTime ExpiresAtUtc,
    int AccessCount,
    bool IsExpired);
