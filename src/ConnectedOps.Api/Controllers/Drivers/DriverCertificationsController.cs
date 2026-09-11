using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Drivers;

[ApiController]
[Route("api/drivers/{driverId:guid}/certifications")]
[Authorize]
public sealed class DriverCertificationsController : ControllerBase
{
    private readonly IDriverCertificationService _certificationService;

    public DriverCertificationsController(IDriverCertificationService certificationService)
    {
        _certificationService = certificationService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.DriverCertifications.View)]
    public async Task<IActionResult> GetCertifications(
        Guid driverId,
        CancellationToken cancellationToken)
    {
        var result = await _certificationService.GetCertificationsAsync(driverId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{certificationId:guid}")]
    [RequirePermission(PermissionKeys.DriverCertifications.View)]
    public async Task<IActionResult> GetById(
        Guid driverId,
        Guid certificationId,
        CancellationToken cancellationToken)
    {
        var result = await _certificationService.GetCertificationByIdAsync(driverId, certificationId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.DriverCertifications.Manage)]
    public async Task<IActionResult> AddCertification(
        Guid driverId,
        [FromBody] CreateDriverCertificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _certificationService.AddCertificationAsync(driverId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { driverId, certificationId = result.Id }, result);
    }

    [HttpPut("{certificationId:guid}")]
    [RequirePermission(PermissionKeys.DriverCertifications.Manage)]
    public async Task<IActionResult> UpdateCertification(
        Guid driverId,
        Guid certificationId,
        [FromBody] UpdateDriverCertificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _certificationService.UpdateCertificationAsync(driverId, certificationId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{certificationId:guid}/deactivate")]
    [RequirePermission(PermissionKeys.DriverCertifications.Manage)]
    public async Task<IActionResult> Deactivate(
        Guid driverId,
        Guid certificationId,
        CancellationToken cancellationToken)
    {
        await _certificationService.DeactivateCertificationAsync(driverId, certificationId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{certificationId:guid}")]
    [RequirePermission(PermissionKeys.DriverCertifications.Manage)]
    public async Task<IActionResult> DeleteCertification(
        Guid driverId,
        Guid certificationId,
        CancellationToken cancellationToken)
    {
        await _certificationService.DeleteCertificationAsync(driverId, certificationId, cancellationToken);
        return NoContent();
    }
}
