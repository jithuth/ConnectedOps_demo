using ConnectedOps.Application.WhiteLabel;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.WhiteLabel;

[ApiController]
[Route("api/white-label")]
[Authorize]
public sealed class WhiteLabelController : ControllerBase
{
    private readonly IWhiteLabelService _whiteLabelService;

    public WhiteLabelController(IWhiteLabelService whiteLabelService)
    {
        _whiteLabelService = whiteLabelService;
    }

    [HttpGet("overview")]
    [RequirePermission(PermissionKeys.WhiteLabel.View)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _whiteLabelService.GetOverviewAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("branding")]
    [RequirePermission(PermissionKeys.WhiteLabel.View)]
    public async Task<IActionResult> GetBranding(CancellationToken cancellationToken)
    {
        var result = await _whiteLabelService.GetBrandingAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("branding")]
    [RequirePermission(PermissionKeys.WhiteLabel.ManageBranding)]
    public async Task<IActionResult> UpdateBranding([FromBody] UpdateBrandingRequest request, CancellationToken cancellationToken)
    {
        var result = await _whiteLabelService.UpdateBrandingAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("domains")]
    [RequirePermission(PermissionKeys.WhiteLabel.ManageDomains)]
    public async Task<IActionResult> RegisterDomain([FromBody] RegisterCustomDomainRequest request, CancellationToken cancellationToken)
    {
        var result = await _whiteLabelService.RegisterCustomDomainAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("domains/verify")]
    [RequirePermission(PermissionKeys.WhiteLabel.ManageDomains)]
    public async Task<IActionResult> VerifyDomain([FromBody] VerifyCustomDomainRequest request, CancellationToken cancellationToken)
    {
        var result = await _whiteLabelService.VerifyCustomDomainAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("audit/generate")]
    [RequirePermission(PermissionKeys.AuditCompliance.GeneratePackage)]
    public async Task<IActionResult> GenerateAuditPackage([FromBody] GenerateAuditPackageRequest request, CancellationToken cancellationToken)
    {
        var result = await _whiteLabelService.GenerateAuditPackageAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("audit/packages")]
    [RequirePermission(PermissionKeys.AuditCompliance.View)]
    public async Task<IActionResult> GetAuditPackages([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _whiteLabelService.GetAuditPackagesPagedAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }
}
