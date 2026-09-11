using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Assets;

[ApiController]
[Route("api/assets/documents")]
[Authorize]
public sealed class AssetDocumentsController : ControllerBase
{
    private readonly IAssetDocumentService _documentService;

    public AssetDocumentsController(IAssetDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("asset/{assetId:guid}")]
    [RequirePermission(PermissionKeys.AssetDocuments.View)]
    public async Task<IActionResult> GetByAsset(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentsByAssetAsync(assetId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.AssetDocuments.Manage)]
    public async Task<IActionResult> AddDocument(
        [FromBody] CreateAssetDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.AddDocumentAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{documentId:guid}")]
    [RequirePermission(PermissionKeys.AssetDocuments.Manage)]
    public async Task<IActionResult> DeleteDocument(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _documentService.DeleteDocumentAsync(documentId, cancellationToken);
        return NoContent();
    }
}
