namespace ConnectedOps.Application.Platform;

public sealed record PlatformSettingsDto(
    string PlatformName,
    string CompanyName,
    string SupportEmail,
    string DefaultLanguage,
    string DefaultTimeZone);

public sealed record UpdatePlatformSettingsRequest(
    string PlatformName,
    string CompanyName,
    string SupportEmail,
    string DefaultLanguage,
    string DefaultTimeZone);

public interface IPlatformSettingsService
{
    Task<PlatformSettingsDto> GetSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<PlatformSettingsDto> UpdateSettingsAsync(
        UpdatePlatformSettingsRequest request,
        CancellationToken cancellationToken = default);
}
