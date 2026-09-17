using ConnectedOps.Application.Integrations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Integrations;

[ApiController]
[Route("api/integrations/erp")]
[Authorize]
public sealed class ErpIntegrationsController : ControllerBase
{
    private readonly IErpExportService _erpService;

    public ErpIntegrationsController(IErpExportService erpService)
    {
        _erpService = erpService;
    }

    [HttpGet("batches")]
    [RequirePermission(PermissionKeys.Integrations.View)]
    public async Task<IActionResult> GetBatches([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _erpService.GetBatchesPagedAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("batches/{id:guid}")]
    [RequirePermission(PermissionKeys.Integrations.View)]
    public async Task<IActionResult> GetBatchById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _erpService.GetBatchByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("batches/generate")]
    [RequirePermission(PermissionKeys.Integrations.ExportErp)]
    public async Task<IActionResult> GenerateBatch([FromBody] GenerateErpBatchRequest request, CancellationToken cancellationToken)
    {
        var result = await _erpService.GenerateBatchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBatchById), new { id = result.Id }, result);
    }

    [HttpPost("batches/{id:guid}/mark-exported")]
    [RequirePermission(PermissionKeys.Integrations.ExportErp)]
    public async Task<IActionResult> MarkBatchExported(Guid id, [FromQuery] string? externalRef, CancellationToken cancellationToken)
    {
        var result = await _erpService.MarkBatchExportedAsync(id, externalRef, cancellationToken);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }
}
