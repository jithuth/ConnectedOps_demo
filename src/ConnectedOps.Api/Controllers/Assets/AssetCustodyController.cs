using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Assets;

[ApiController]
[Route("api/assets/custody")]
[Authorize]
public sealed class AssetCustodyController : ControllerBase
{
    private readonly IAssetCustodyService _custodyService;

    public AssetCustodyController(IAssetCustodyService custodyService)
    {
        _custodyService = custodyService;
    }

    // Employee Assignments
    [HttpGet("employees/asset/{assetId:guid}")]
    [RequirePermission(PermissionKeys.AssetCustody.View)]
    public async Task<IActionResult> GetEmployeeAssignmentsByAsset(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.GetEmployeeAssignmentsByAssetAsync(assetId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("employees/active/{employeeId:guid}")]
    [RequirePermission(PermissionKeys.AssetCustody.View)]
    public async Task<IActionResult> GetActiveAssignmentsByEmployee(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.GetActiveAssignmentsByEmployeeAsync(employeeId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("employees/assign")]
    [RequirePermission(PermissionKeys.AssetCustody.Manage)]
    public async Task<IActionResult> AssignToEmployee(
        [FromBody] AssignAssetToEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.AssignToEmployeeAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("employees/{assignmentId:guid}/return")]
    [RequirePermission(PermissionKeys.AssetCustody.Manage)]
    public async Task<IActionResult> ReturnFromEmployee(
        Guid assignmentId,
        [FromBody] ReturnAssetFromEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.ReturnFromEmployeeAsync(assignmentId, request, cancellationToken);
        return Ok(result);
    }

    // Vehicle Assignments
    [HttpGet("vehicles/asset/{assetId:guid}")]
    [RequirePermission(PermissionKeys.AssetCustody.View)]
    public async Task<IActionResult> GetVehicleAssignmentsByAsset(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.GetVehicleAssignmentsByAssetAsync(assetId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicles/active/{vehicleId:guid}")]
    [RequirePermission(PermissionKeys.AssetCustody.View)]
    public async Task<IActionResult> GetActiveAssignmentsByVehicle(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.GetActiveAssignmentsByVehicleAsync(vehicleId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("vehicles/assign")]
    [RequirePermission(PermissionKeys.AssetCustody.Manage)]
    public async Task<IActionResult> AssignToVehicle(
        [FromBody] AssignAssetToVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.AssignToVehicleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("vehicles/{assignmentId:guid}/remove")]
    [RequirePermission(PermissionKeys.AssetCustody.Manage)]
    public async Task<IActionResult> RemoveFromVehicle(
        Guid assignmentId,
        [FromBody] RemoveAssetFromVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.RemoveFromVehicleAsync(assignmentId, request, cancellationToken);
        return Ok(result);
    }

    // Usage Sessions (Check-out / Check-in)
    [HttpGet("sessions")]
    [RequirePermission(PermissionKeys.AssetCustody.View)]
    public async Task<IActionResult> GetUsageSessions(
        [FromQuery] Guid? assetId,
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? vehicleId,
        [FromQuery] bool? activeOnly,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _custodyService.GetUsageSessionsPagedAsync(assetId, employeeId, vehicleId, activeOnly, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("sessions/active/{assetId:guid}")]
    [RequirePermission(PermissionKeys.AssetCustody.View)]
    public async Task<IActionResult> GetActiveSessionByAsset(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.GetActiveSessionByAssetAsync(assetId, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("sessions/checkout")]
    [RequirePermission(PermissionKeys.AssetCustody.CheckOut)]
    public async Task<IActionResult> CheckOut(
        [FromBody] StartAssetUsageSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.CheckOutAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sessions/{sessionId:guid}/checkin")]
    [RequirePermission(PermissionKeys.AssetCustody.CheckIn)]
    public async Task<IActionResult> CheckIn(
        Guid sessionId,
        [FromBody] EndAssetUsageSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _custodyService.CheckInAsync(sessionId, request, cancellationToken);
        return Ok(result);
    }
}
