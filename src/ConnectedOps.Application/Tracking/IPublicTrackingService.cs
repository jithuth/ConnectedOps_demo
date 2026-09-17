namespace ConnectedOps.Application.Tracking;

public interface IPublicTrackingService
{
    Task<PublicTrackingTokenDto> GenerateTokenForJobAsync(GenerateTrackingTokenRequest request, CancellationToken cancellationToken = default);

    Task<PublicTrackingInfoDto?> GetPublicTrackingInfoAsync(string token, CancellationToken cancellationToken = default);

    Task<bool> SubmitDeliveryRatingAsync(string token, SubmitDeliveryRatingRequest request, CancellationToken cancellationToken = default);
}
