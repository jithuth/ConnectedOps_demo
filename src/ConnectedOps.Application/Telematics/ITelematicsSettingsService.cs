namespace ConnectedOps.Application.Telematics;

public interface ITelematicsSettingsService
{
    Task<TelematicsSettingsDto> GetSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<TelematicsSettingsDto> UpdateSettingsAsync(
        UpdateTelematicsSettingsRequest request,
        CancellationToken cancellationToken = default);
}
