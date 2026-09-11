using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Drivers;

[ApiController]
[Route("api/drivers/{driverId:guid}/documents")]
[Authorize]
public sealed class DriverDocumentsController : ControllerBase
{
    private readonly IDriverDocumentService _documentService;

    public DriverDocumentsController(IDriverDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.DriverDocuments.View)]
    public async Task<IActionResult> GetDocuments(
        Guid driverId,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentsAsync(driverId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{documentId:guid}")]
    [RequirePermission(PermissionKeys.DriverDocuments.View)]
    public async Task<IActionResult> GetById(
        Guid driverId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentByIdAsync(driverId, documentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.DriverDocuments.Manage)]
    public async Task<IActionResult> AddDocument(
        Guid driverId,
        [FromBody] CreateDriverDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.AddDocumentAsync(driverId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { driverId, documentId = result.Id }, result);
    }

    [HttpPut("{documentId:guid}")]
    [RequirePermission(PermissionKeys.DriverDocuments.Manage)]
    public async Task<IActionResult> UpdateDocument(
        Guid driverId,
        Guid documentId,
        [FromBody] UpdateDriverDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.UpdateDocumentAsync(driverId, documentId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{documentId:guid}/deactivate")]
    [RequirePermission(PermissionKeys.DriverDocuments.Manage)]
    public async Task<IActionResult> Deactivate(
        Guid driverId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _documentService.DeactivateDocumentAsync(driverId, documentId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{documentId:guid}")]
    [RequirePermission(PermissionKeys.DriverDocuments.Manage)]
    public async Task<IActionResult> DeleteDocument(
        Guid driverId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _documentService.DeleteDocumentAsync(driverId, documentId, cancellationToken);
        return NoContent();
    }
}
