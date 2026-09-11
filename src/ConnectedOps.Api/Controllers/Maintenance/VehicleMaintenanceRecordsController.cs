using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Maintenance;

[ApiController]
[Authorize]
public sealed class VehicleMaintenanceRecordsController : ControllerBase
{
    private readonly IMaintenanceRecordService _recordService;

    public VehicleMaintenanceRecordsController(IMaintenanceRecordService recordService)
    {
        _recordService = recordService;
    }

    [HttpGet("api/maintenance/records")]
    [RequirePermission(PermissionKeys.MaintenanceRecords.View)]
    public async Task<IActionResult> GetRecords(
        [FromQuery] MaintenanceRecordQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.GetRecordsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/maintenance/records/{id:guid}")]
    [HttpGet("api/maintenance/{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceRecords.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/vehicles/{vehicleId:guid}/maintenance")]
    [RequirePermission(PermissionKeys.MaintenanceRecords.View)]
    public async Task<IActionResult> GetVehicleMaintenanceHistory(
        Guid vehicleId,
        [FromQuery] MaintenanceRecordQueryParameters? query,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.GetVehicleMaintenanceHistoryAsync(vehicleId, query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/maintenance/records")]
    [RequirePermission(PermissionKeys.MaintenanceRecords.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMaintenanceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("api/maintenance/records/{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceRecords.Edit)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMaintenanceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/maintenance/records/{id:guid}/start")]
    [RequirePermission(PermissionKeys.MaintenanceRecords.Edit)]
    public async Task<IActionResult> StartService(
        Guid id,
        [FromBody] StartMaintenanceRecordRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.StartServiceAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/maintenance/records/{id:guid}/complete")]
    [RequirePermission(PermissionKeys.MaintenanceRecords.Complete)]
    public async Task<IActionResult> CompleteService(
        Guid id,
        [FromBody] CompleteMaintenanceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.CompleteServiceAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/maintenance/records/{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.MaintenanceRecords.Cancel)]
    public async Task<IActionResult> CancelService(
        Guid id,
        [FromBody] CancelMaintenanceRecordRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.CancelServiceAsync(id, request, cancellationToken);
        return Ok(result);
    }

    // Tasks
    [HttpPost("api/maintenance/records/{recordId:guid}/tasks")]
    [RequirePermission(PermissionKeys.MaintenanceTasks.Manage)]
    public async Task<IActionResult> AddTask(
        Guid recordId,
        [FromBody] AddMaintenanceTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.AddTaskAsync(recordId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("api/maintenance/records/{recordId:guid}/tasks/{taskId:guid}/status")]
    [RequirePermission(PermissionKeys.MaintenanceTasks.Manage)]
    public async Task<IActionResult> UpdateTaskStatus(
        Guid recordId,
        Guid taskId,
        [FromBody] UpdateMaintenanceTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.UpdateTaskStatusAsync(recordId, taskId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/maintenance/records/{recordId:guid}/tasks/{taskId:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceTasks.Manage)]
    public async Task<IActionResult> DeleteTask(
        Guid recordId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        await _recordService.DeleteTaskAsync(recordId, taskId, cancellationToken);
        return NoContent();
    }

    // Parts
    [HttpPost("api/maintenance/records/{recordId:guid}/parts")]
    [RequirePermission(PermissionKeys.MaintenanceParts.Manage)]
    public async Task<IActionResult> AddPart(
        Guid recordId,
        [FromBody] AddMaintenancePartRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.AddPartAsync(recordId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/maintenance/records/{recordId:guid}/parts/{partId:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceParts.Manage)]
    public async Task<IActionResult> DeletePart(
        Guid recordId,
        Guid partId,
        CancellationToken cancellationToken)
    {
        await _recordService.DeletePartAsync(recordId, partId, cancellationToken);
        return NoContent();
    }

    // Labour
    [HttpPost("api/maintenance/records/{recordId:guid}/labour")]
    [RequirePermission(PermissionKeys.MaintenanceLabour.Manage)]
    public async Task<IActionResult> AddLabour(
        Guid recordId,
        [FromBody] AddMaintenanceLabourRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.AddLabourAsync(recordId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/maintenance/records/{recordId:guid}/labour/{labourId:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceLabour.Manage)]
    public async Task<IActionResult> DeleteLabour(
        Guid recordId,
        Guid labourId,
        CancellationToken cancellationToken)
    {
        await _recordService.DeleteLabourAsync(recordId, labourId, cancellationToken);
        return NoContent();
    }

    // Expenses
    [HttpPost("api/maintenance/records/{recordId:guid}/expenses")]
    [RequirePermission(PermissionKeys.MaintenanceExpenses.Manage)]
    public async Task<IActionResult> AddExpense(
        Guid recordId,
        [FromBody] AddMaintenanceExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.AddExpenseAsync(recordId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/maintenance/records/{recordId:guid}/expenses/{expenseId:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceExpenses.Manage)]
    public async Task<IActionResult> DeleteExpense(
        Guid recordId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        await _recordService.DeleteExpenseAsync(recordId, expenseId, cancellationToken);
        return NoContent();
    }

    // Documents
    [HttpPost("api/maintenance/records/{recordId:guid}/documents")]
    [RequirePermission(PermissionKeys.MaintenanceDocuments.Manage)]
    public async Task<IActionResult> AddDocument(
        Guid recordId,
        [FromBody] AddMaintenanceDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordService.AddDocumentAsync(recordId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/maintenance/records/{recordId:guid}/documents/{documentId:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceDocuments.Manage)]
    public async Task<IActionResult> DeleteDocument(
        Guid recordId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _recordService.DeleteDocumentAsync(recordId, documentId, cancellationToken);
        return NoContent();
    }
}
