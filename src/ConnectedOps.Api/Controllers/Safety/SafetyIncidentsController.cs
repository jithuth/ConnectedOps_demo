using ConnectedOps.Application.Safety;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Safety;

[ApiController]
[Route("api/safety/incidents")]
[Authorize]
public sealed class SafetyIncidentsController : ControllerBase
{
    private readonly ISafetyIncidentService _incidentService;

    public SafetyIncidentsController(ISafetyIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.SafetyIncidents.View)]
    public async Task<IActionResult> GetAll([FromQuery] SafetyIncidentFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _incidentService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.SafetyIncidents.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _incidentService.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.SafetyIncidents.Create)]
    public async Task<IActionResult> Create([FromBody] CreateSafetyIncidentRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Edit)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSafetyIncidentRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/start-investigation")]
    [RequirePermission(PermissionKeys.SafetyInvestigations.Manage)]
    public async Task<IActionResult> StartInvestigation(Guid id, [FromBody] StartSafetyInvestigationRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.StartInvestigationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete-investigation")]
    [RequirePermission(PermissionKeys.SafetyInvestigations.Manage)]
    public async Task<IActionResult> CompleteInvestigation(Guid id, [FromBody] CompleteSafetyInvestigationRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.CompleteInvestigationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/close")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Close)]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseSafetyIncidentRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.CloseAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Cancel)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _incidentService.CancelAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/participants")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Edit)]
    public async Task<IActionResult> AddParticipant(Guid id, [FromBody] AddIncidentParticipantRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.AddParticipantAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/participants/{participantId:guid}")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Edit)]
    public async Task<IActionResult> RemoveParticipant(Guid id, Guid participantId, CancellationToken cancellationToken)
    {
        await _incidentService.RemoveParticipantAsync(id, participantId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/vehicles")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Edit)]
    public async Task<IActionResult> AddVehicle(Guid id, [FromBody] AddIncidentVehicleRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.AddVehicleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/vehicles/{incidentVehicleId:guid}")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Edit)]
    public async Task<IActionResult> RemoveVehicle(Guid id, Guid incidentVehicleId, CancellationToken cancellationToken)
    {
        await _incidentService.RemoveVehicleAsync(id, incidentVehicleId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/assets")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Edit)]
    public async Task<IActionResult> AddAsset(Guid id, [FromBody] AddIncidentAssetRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.AddAssetAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/assets/{incidentAssetId:guid}")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Edit)]
    public async Task<IActionResult> RemoveAsset(Guid id, Guid incidentAssetId, CancellationToken cancellationToken)
    {
        await _incidentService.RemoveAssetAsync(id, incidentAssetId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/evidence")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Edit)]
    public async Task<IActionResult> AddEvidence(Guid id, [FromBody] AddIncidentEvidenceRequest request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.AddEvidenceAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/evidence/{evidenceId:guid}")]
    [RequirePermission(PermissionKeys.SafetyIncidents.Edit)]
    public async Task<IActionResult> RemoveEvidence(Guid id, Guid evidenceId, CancellationToken cancellationToken)
    {
        await _incidentService.RemoveEvidenceAsync(id, evidenceId, cancellationToken);
        return NoContent();
    }
}
