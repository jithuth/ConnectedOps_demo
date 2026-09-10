using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Platform;

public sealed class PlatformSettingsService : IPlatformSettingsService
{
    private readonly ConnectedOpsDbContext _dbContext;

    public PlatformSettingsService(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformSettingsDto> GetSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);

        return new PlatformSettingsDto(
            settings.PlatformName,
            settings.CompanyName,
            settings.SupportEmail,
            settings.DefaultLanguage,
            settings.DefaultTimeZone);
    }

    public async Task<PlatformSettingsDto> UpdateSettingsAsync(
        UpdatePlatformSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);

        settings.Update(
            request.PlatformName,
            request.CompanyName,
            request.SupportEmail,
            request.DefaultLanguage,
            request.DefaultTimeZone);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PlatformSettingsDto(
            settings.PlatformName,
            settings.CompanyName,
            settings.SupportEmail,
            settings.DefaultLanguage,
            settings.DefaultTimeZone);
    }

    private async Task<PlatformSettings> GetOrCreateSettingsAsync(
        CancellationToken cancellationToken)
    {
        var settings = await _dbContext.PlatformSettings
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new PlatformSettings(
                "ConnectedOps",
                "ConnectedOps Technologies",
                "support@connectedops.com",
                "en",
                "UTC");

            _dbContext.PlatformSettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }
}
