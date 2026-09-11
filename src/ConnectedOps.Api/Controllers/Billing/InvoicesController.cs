using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Billing;

[ApiController]
[Route("api/billing/invoices")]
[Authorize]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Invoices.View)]
    public async Task<ActionResult<IReadOnlyCollection<InvoiceListItemDto>>> GetInvoices(
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetInvoicesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Invoices.View)]
    public async Task<ActionResult<InvoiceDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetInvoiceByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Invoices.Create)]
    public async Task<ActionResult<InvoiceDto>> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.CreateInvoiceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/issue")]
    [RequirePermission(PermissionKeys.Invoices.Manage)]
    public async Task<ActionResult<InvoiceDto>> Issue(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.IssueInvoiceAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/void")]
    [RequirePermission(PermissionKeys.Invoices.Manage)]
    public async Task<ActionResult<InvoiceDto>> Void(
        Guid id,
        [FromBody] VoidInvoiceRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.VoidInvoiceAsync(id, request?.Reason, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/billing-info")]
    [RequirePermission(PermissionKeys.Invoices.Manage)]
    public async Task<IActionResult> UpdateBillingInfo(
        Guid id,
        [FromBody] UpdateInvoiceBillingInfoRequest request,
        CancellationToken cancellationToken)
    {
        await _invoiceService.UpdateBillingInfoAsync(id, request, cancellationToken);
        return NoContent();
    }
}

public sealed record VoidInvoiceRequest(string? Reason);
