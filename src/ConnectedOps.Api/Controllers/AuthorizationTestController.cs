using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/test")]
[Authorize]
public sealed class AuthorizationTestController
    : ControllerBase
{
    private readonly ICurrentTenantContext _currentTenant;

    public AuthorizationTestController(
        ICurrentTenantContext currentTenant)
    {
        _currentTenant = currentTenant;
    }

    [HttpGet("context")]
    public IActionResult Context()
    {
        return Ok(new
        {
            _currentTenant.IsAuthenticated,
            _currentTenant.UserId,
            _currentTenant.TenantId,
            _currentTenant.TenantUserId
        });
    }

    [HttpGet("vehicles")]
    [RequirePermission(
        PermissionKeys.Vehicles.View)]
    public IActionResult Vehicles()
    {
        return Ok(new
        {
            message =
                "Vehicles.View permission granted.",

            tenantId =
                _currentTenant.TenantId
        });
    }

    [HttpGet("delete-vehicle")]
    [RequirePermission(
        PermissionKeys.Vehicles.Delete)]
    public IActionResult DeleteVehicleTest()
    {
        return Ok(new
        {
            message =
                "Vehicles.Delete permission granted."
        });
    }
}