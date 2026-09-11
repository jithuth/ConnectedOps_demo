using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Vehicles;

[ApiController]
[Route("api/vehicles/{vehicleId:guid}/documents")]
[Authorize]
public sealed class VehicleDocumentsController : ControllerBase
{
    private readonly IVehicleDocumentService _documentService;

    public VehicleDocumentsController(IVehicleDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Vehicles.View)]
    public async Task<IActionResult> GetAll(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentsAsync(vehicleId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{documentId:guid}")]
    [RequirePermission(PermissionKeys.Vehicles.View)]
    public async Task<IActionResult> GetById(
        Guid vehicleId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentByIdAsync(vehicleId, documentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Vehicles.ManageDocuments)]
    public async Task<IActionResult> Add(
        Guid vehicleId,
        [FromBody] CreateVehicleDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.AddDocumentAsync(vehicleId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { vehicleId, documentId = result.Id }, result);
    }

    [HttpPut("{documentId:guid}")]
    [RequirePermission(PermissionKeys.Vehicles.ManageDocuments)]
    public async Task<IActionResult> Update(
        Guid vehicleId,
        Guid documentId,
        [FromBody] UpdateVehicleDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.UpdateDocumentAsync(vehicleId, documentId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{documentId:guid}")]
    [RequirePermission(PermissionKeys.Vehicles.ManageDocuments)]
    public async Task<IActionResult> Delete(
        Guid vehicleId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _documentService.DeleteDocumentAsync(vehicleId, documentId, cancellationToken);
        return NoContent();
    }
}
