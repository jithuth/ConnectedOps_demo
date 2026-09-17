using ConnectedOps.Application.Integrations;
using ConnectedOps.Domain.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Integrations;

[Authorize]
public class ApiKeysModel : PageModel
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysModel(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    public IReadOnlyCollection<TenantApiKeyDto> ApiKeys { get; private set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? NewlyCreatedKeyPlaintext { get; set; }

    [TempData]
    public string? NewlyCreatedKeyName { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ApiKeys = await _apiKeyService.GetApiKeysAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string name,
        List<ApiKeyScope> scopes,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "API Key name is required.";
                return RedirectToPage();
            }

            if (scopes == null || scopes.Count == 0)
            {
                ErrorMessage = "Select at least one permission scope.";
                return RedirectToPage();
            }

            var result = await _apiKeyService.CreateApiKeyAsync(
                new CreateApiKeyRequest(name, scopes, expiresAtUtc),
                cancellationToken);

            SuccessMessage = $"API Key '{result.Name}' created successfully.";
            NewlyCreatedKeyPlaintext = result.PlaintextApiKey;
            NewlyCreatedKeyName = result.Name;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid apiKeyId, CancellationToken cancellationToken)
    {
        try
        {
            await _apiKeyService.RevokeApiKeyAsync(apiKeyId, cancellationToken);
            SuccessMessage = "API Key has been revoked immediately.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
