using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Assets;

[ApiController]
[Route("api/assets")]
[Authorize]
public sealed class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly IAssetActivityTimelineService _timelineService;
    private readonly IAssetIdentifierService _identifierService;
    private readonly IAssetQrCodeService _qrCodeService;

    public AssetsController(
        IAssetService assetService,
        IAssetActivityTimelineService timelineService,
        IAssetIdentifierService identifierService,
        IAssetQrCodeService qrCodeService)
    {
        _assetService = assetService;
        _timelineService = timelineService;
        _identifierService = identifierService;
        _qrCodeService = qrCodeService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Assets.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] AssetListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _assetService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Assets.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _assetService.GetByIdAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("scan/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ScanToken(
        string token,
        CancellationToken cancellationToken)
    {
        var result = await _identifierService.ScanByTokenAsync(token, cancellationToken);
        if (result is null) return NotFound(new { message = "Asset tag is invalid or not found." });
        return Ok(result);
    }

    [HttpGet("{id:guid}/qr-code")]
    [RequirePermission(PermissionKeys.Assets.View)]
    public async Task<IActionResult> GetQrCode(
        Guid id,
        CancellationToken cancellationToken)
    {
        var asset = await _assetService.GetByIdAsync(id, cancellationToken);
        if (asset is null) return NotFound();
        var qrPayload = $"https://app.connectedops.io/assets/scan/{asset.Id}";
        var pngBytes = _qrCodeService.GeneratePngQrCode(qrPayload);
        return File(pngBytes, "image/png", $"asset-{asset.AssetNumber}-qr.png");
    }

    [HttpGet("{id:guid}/timeline")]
    [RequirePermission(PermissionKeys.Assets.View)]
    public async Task<IActionResult> GetTimeline(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _timelineService.GetTimelineByAssetAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Assets.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assetService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Assets.Edit)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assetService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Assets.Delete)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _assetService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    [RequirePermission(PermissionKeys.Assets.ChangeStatus)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeAssetStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assetService.ChangeStatusAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/location")]
    [RequirePermission(PermissionKeys.Assets.ManageLocation)]
    public async Task<IActionResult> ChangeLocation(
        Guid id,
        [FromBody] UpdateAssetLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assetService.UpdateLocationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/notes")]
    [RequirePermission(PermissionKeys.Assets.Edit)]
    public async Task<IActionResult> AddNote(
        Guid id,
        [FromBody] AddAssetNoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assetService.AddNoteAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/identifiers")]
    [RequirePermission(PermissionKeys.AssetIdentifiers.View)]
    public async Task<IActionResult> GetIdentifiers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _identifierService.GetIdentifiersByAssetAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/identifiers")]
    [RequirePermission(PermissionKeys.AssetIdentifiers.Manage)]
    public async Task<IActionResult> AddIdentifier(
        Guid id,
        [FromBody] CreateAssetIdentifierRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identifierService.CreateIdentifierAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/identifiers/generate-qr")]
    [RequirePermission(PermissionKeys.AssetIdentifiers.Manage)]
    public async Task<IActionResult> GenerateQrIdentifier(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _identifierService.GenerateQrTokenAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("identifiers/{identifierId:guid}")]
    [RequirePermission(PermissionKeys.AssetIdentifiers.Manage)]
    public async Task<IActionResult> DeleteIdentifier(
        Guid identifierId,
        CancellationToken cancellationToken)
    {
        await _identifierService.DeactivateIdentifierAsync(identifierId, cancellationToken);
        return NoContent();
    }
}
