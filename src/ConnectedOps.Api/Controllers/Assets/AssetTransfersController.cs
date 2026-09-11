using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Assets;

[ApiController]
[Route("api/assets/transfers")]
[Authorize]
public sealed class AssetTransfersController : ControllerBase
{
    private readonly IAssetTransferService _transferService;

    public AssetTransfersController(IAssetTransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.AssetTransfers.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] AssetTransferFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.AssetTransfers.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.GetByIdAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("asset/{assetId:guid}")]
    [RequirePermission(PermissionKeys.AssetTransfers.View)]
    public async Task<IActionResult> GetByAsset(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.GetTransfersByAssetAsync(assetId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.AssetTransfers.Create)]
    public async Task<IActionResult> Initiate(
        [FromBody] CreateAssetTransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.CreateTransferAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/complete")]
    [RequirePermission(PermissionKeys.AssetTransfers.Complete)]
    public async Task<IActionResult> Complete(
        Guid id,
        [FromBody] CompleteAssetTransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.CompleteTransferAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.AssetTransfers.Cancel)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelAssetTransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.CancelTransferAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
