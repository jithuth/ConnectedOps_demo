using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Demo;
using ConnectedOps.Application.Tracking;
using ConnectedOps.Domain.Dispatch;
using ConnectedOps.Domain.Tracking;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Tracking;

public sealed class PublicTrackingService : IPublicTrackingService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDemoFleetSimulator _demoFleetSimulator;
    private readonly ILogger<PublicTrackingService> _logger;

    public PublicTrackingService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDemoFleetSimulator demoFleetSimulator,
        ILogger<PublicTrackingService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _demoFleetSimulator = demoFleetSimulator;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required to generate tracking tokens.");
    }

    public async Task<PublicTrackingTokenDto> GenerateTokenForJobAsync(GenerateTrackingTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var job = await _dbContext.DispatchJobs
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == request.DispatchJobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dispatch job {request.DispatchJobId} was not found.");

        // Check if there is an existing unexpired token
        var existingToken = await _dbContext.PublicTrackingTokens
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.DispatchJobId == job.Id && t.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);

        if (existingToken != null)
        {
            return new PublicTrackingTokenDto(
                existingToken.Id,
                existingToken.DispatchJobId,
                existingToken.Token,
                existingToken.CustomerName,
                existingToken.CustomerPhone,
                existingToken.ExpiresAtUtc,
                existingToken.AccessCount,
                existingToken.IsExpired);
        }

        var expiresAt = DateTime.UtcNow.AddHours(request.ExpiryHours > 0 ? request.ExpiryHours : 48);
        var token = new PublicTrackingToken(
            tenantId,
            job.Id,
            job.CustomerName,
            job.CustomerPhone,
            expiresAt);

        _dbContext.PublicTrackingTokens.Add(token);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated public tracking token {Token} for job {JobId}", token.Token, job.Id);

        return new PublicTrackingTokenDto(
            token.Id,
            token.DispatchJobId,
            token.Token,
            token.CustomerName,
            token.CustomerPhone,
            token.ExpiresAtUtc,
            token.AccessCount,
            token.IsExpired);
    }

    public async Task<PublicTrackingInfoDto?> GetPublicTrackingInfoAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var tokenEntity = await _dbContext.PublicTrackingTokens
            .IgnoreQueryFilters()
            .Include(t => t.DispatchJob)
                .ThenInclude(j => j.AssignedVehicle)
            .Include(t => t.DispatchJob)
                .ThenInclude(j => j.AssignedDriver)
            .FirstOrDefaultAsync(t => t.Token == token.Trim(), cancellationToken);

        if (tokenEntity == null || tokenEntity.IsExpired)
        {
            return null;
        }

        tokenEntity.RecordAccess();
        await _dbContext.SaveChangesAsync(cancellationToken);

        var job = tokenEntity.DispatchJob;
        var vehicle = job?.AssignedVehicle;
        var driver = job?.AssignedDriver;

        double? vehicleLat = null;
        double? vehicleLng = null;
        double? vehicleSpeed = null;

        // Try to fetch live simulated coordinates if vehicle is assigned
        if (vehicle != null)
        {
            try
            {
                var demoVehicles = await _demoFleetSimulator.GetDemoVehiclesAsync(cancellationToken);
                var live = demoVehicles.FirstOrDefault(v => v.VehicleId == vehicle.Id);
                if (live != null)
                {
                    vehicleLat = live.Latitude;
                    vehicleLng = live.Longitude;
                    vehicleSpeed = (double)live.SpeedKph;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Live telemetry fetch skipped for vehicle {VehicleId}", vehicle.Id);
            }

            // Fallback default coordinates close to job destination if vehicle has no telemetry yet
            if (!vehicleLat.HasValue && job != null)
            {
                // Position ~2.5km away towards Dubai/Abu Dhabi corridor
                vehicleLat = job.Latitude - 0.015;
                vehicleLng = job.Longitude - 0.012;
                vehicleSpeed = 42.0;
            }
        }

        // Compute estimated remaining minutes and arrival UTC
        int? minutesRemaining = null;
        DateTime? estimatedArrival = null;

        var isDelivered = job?.Status == DispatchJobStatus.Completed;

        if (job != null && !isDelivered && vehicleLat.HasValue && vehicleLng.HasValue)
        {
            var distanceKm = CalculateHaversineDistanceKm(vehicleLat.Value, vehicleLng.Value, job.Latitude, job.Longitude);
            var effectiveSpeed = vehicleSpeed > 10 ? vehicleSpeed.Value : 35.0; // km/h
            var hours = distanceKm / effectiveSpeed;
            minutesRemaining = (int)Math.Max(5, Math.Round(hours * 60.0));
            estimatedArrival = DateTime.UtcNow.AddMinutes(minutesRemaining.Value);
        }

        return new PublicTrackingInfoDto(
            TrackingToken: tokenEntity.Token,
            JobId: job?.Id ?? Guid.Empty,
            JobTitle: job?.Title ?? "Delivery Order",
            CustomerName: tokenEntity.CustomerName,
            DeliveryAddress: job?.Address ?? "Customer Destination",
            DestinationLatitude: job?.Latitude ?? 25.2048,
            DestinationLongitude: job?.Longitude ?? 55.2708,
            JobStatus: job?.Status.ToString() ?? "Scheduled",
            VehiclePlateNumber: vehicle?.RegistrationNumber,
            VehicleMakeModel: vehicle != null ? $"{vehicle.VehicleNumber}".Trim() : null,
            CurrentVehicleLatitude: vehicleLat,
            CurrentVehicleLongitude: vehicleLng,
            CurrentVehicleSpeedKmh: vehicleSpeed,
            DriverFirstName: driver?.FirstName,
            EstimatedArrivalUtc: estimatedArrival,
            EstimatedMinutesRemaining: minutesRemaining,
            Rating: tokenEntity.Rating,
            FeedbackComment: tokenEntity.FeedbackComment,
            IsDelivered: isDelivered);
    }

    public async Task<bool> SubmitDeliveryRatingAsync(string token, SubmitDeliveryRatingRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var tokenEntity = await _dbContext.PublicTrackingTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Token == token.Trim(), cancellationToken);

        if (tokenEntity == null || tokenEntity.IsExpired)
        {
            return false;
        }

        tokenEntity.SubmitFeedback(request.Rating, request.FeedbackComment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Delivery rating {Rating} submitted for tracking token {Token}", request.Rating, token);
        return true;
    }

    private static double CalculateHaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in km
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
