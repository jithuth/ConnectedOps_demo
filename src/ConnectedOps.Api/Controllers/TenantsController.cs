using ConnectedOps.Application.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantProvisioningService _tenantProvisioningService;

    public TenantsController(
        ITenantProvisioningService tenantProvisioningService)
    {
        _tenantProvisioningService =
            tenantProvisioningService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _tenantProvisioningService.CreateAsync(
                request,
                cancellationToken);

        return Ok(result);
    }
}