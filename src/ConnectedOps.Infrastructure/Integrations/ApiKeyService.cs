using System.Security.Cryptography;
using System.Text;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Integrations;
using ConnectedOps.Domain.Integrations;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Integrations;

public sealed class ApiKeyService : IApiKeyService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ILogger<ApiKeyService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required for API key operations.");
    }

    public async Task<IReadOnlyCollection<TenantApiKeyDto>> GetApiKeysAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var list = await _dbContext.TenantApiKeys
            .AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(k => new TenantApiKeyDto(
            k.Id,
            k.Name,
            k.KeyPrefix,
            k.GetScopes().ToList(),
            k.ExpiresAtUtc,
            k.LastUsedAtUtc,
            k.IsActive && !k.IsRevoked,
            k.IsExpired,
            k.CreatedAtUtc)).ToList();
    }

    public async Task<ApiKeyCreatedResultDto> CreateApiKeyAsync(CreateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var rawKey = GeneratePlaintextKey();
        var keyPrefix = rawKey[..14] + "...";
        var keyHash = ComputeSha256Hash(rawKey);

        var apiKey = new TenantApiKey(
            tenantId,
            request.Name,
            keyPrefix,
            keyHash,
            request.Scopes,
            request.ExpiresAtUtc);

        _dbContext.TenantApiKeys.Add(apiKey);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated API key '{Name}' (Prefix: {Prefix}) for tenant {TenantId}", apiKey.Name, apiKey.KeyPrefix, tenantId);

        return new ApiKeyCreatedResultDto(
            apiKey.Id,
            apiKey.Name,
            apiKey.KeyPrefix,
            rawKey, // Exposed only on generation
            apiKey.GetScopes().ToList(),
            apiKey.ExpiresAtUtc);
    }

    public async Task<bool> RevokeApiKeyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var apiKey = await _dbContext.TenantApiKeys
            .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Id == id, cancellationToken);

        if (apiKey == null) return false;

        apiKey.Revoke(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked API key {Id} for tenant {TenantId}", id, tenantId);
        return true;
    }

    public async Task<(bool IsValid, Guid TenantId, List<ApiKeyScope> Scopes)> ValidateApiKeyAsync(string plaintextApiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plaintextApiKey))
            return (false, Guid.Empty, []);

        var hash = ComputeSha256Hash(plaintextApiKey.Trim());

        var apiKey = await _dbContext.TenantApiKeys
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(k => k.KeyHash == hash, cancellationToken);

        if (apiKey == null || !apiKey.IsValid)
        {
            return (false, Guid.Empty, []);
        }

        apiKey.RecordUse();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, apiKey.TenantId, apiKey.GetScopes().ToList());
    }

    private static string GeneratePlaintextKey()
    {
        var bytes = new byte[24];
        RandomNumberGenerator.Fill(bytes);
        return "co_live_" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
