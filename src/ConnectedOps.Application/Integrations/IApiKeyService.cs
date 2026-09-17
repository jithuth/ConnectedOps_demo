using ConnectedOps.Domain.Integrations;

namespace ConnectedOps.Application.Integrations;

public interface IApiKeyService
{
    Task<IReadOnlyCollection<TenantApiKeyDto>> GetApiKeysAsync(CancellationToken cancellationToken = default);

    Task<ApiKeyCreatedResultDto> CreateApiKeyAsync(CreateApiKeyRequest request, CancellationToken cancellationToken = default);

    Task<bool> RevokeApiKeyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(bool IsValid, Guid TenantId, List<ApiKeyScope> Scopes)> ValidateApiKeyAsync(string plaintextApiKey, CancellationToken cancellationToken = default);
}
