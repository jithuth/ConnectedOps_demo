using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Fuel;

[ApiController]
[Route("api/fuel/cards")]
[Authorize]
public sealed class FuelCardsController : ControllerBase
{
    private readonly IFuelCardService _cardService;

    public FuelCardsController(IFuelCardService cardService)
    {
        _cardService = cardService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FuelCards.View)]
    public async Task<IActionResult> GetCards(
        [FromQuery] FuelCardQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _cardService.GetCardsPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("all")]
    [RequirePermission(PermissionKeys.FuelCards.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await _cardService.GetAllAsync(activeOnly, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.FuelCards.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _cardService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.FuelCards.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFuelCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cardService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.FuelCards.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFuelCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cardService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [RequirePermission(PermissionKeys.FuelCards.Manage)]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] UpdateFuelCardStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cardService.SetStatusAsync(id, request.Status, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.FuelCards.Manage)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _cardService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
