using ConnectedOps.Application.Integrations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Integrations;

[ApiController]
[Route("api/integrations/api-keys")]
[Authorize]
public sealed class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Integrations.View)]
    public async Task<IActionResult> GetApiKeys(CancellationToken cancellationToken)
    {
        var result = await _apiKeyService.GetApiKeysAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Integrations.ManageApiKeys)]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var result = await _apiKeyService.CreateApiKeyAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/revoke")]
    [RequirePermission(PermissionKeys.Integrations.ManageApiKeys)]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken cancellationToken)
    {
        var result = await _apiKeyService.RevokeApiKeyAsync(id, cancellationToken);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }
}
