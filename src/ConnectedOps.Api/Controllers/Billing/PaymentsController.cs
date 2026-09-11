using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Billing;

[ApiController]
[Route("api/billing/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Payments.View)]
    public async Task<ActionResult<IReadOnlyCollection<PaymentTransactionDto>>> GetPayments(
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPaymentsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Payments.View)]
    public async Task<ActionResult<PaymentTransactionDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPaymentByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Payments.Create)]
    public async Task<ActionResult<PaymentTransactionDto>> RecordPayment(
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.RecordPaymentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/refund")]
    [RequirePermission(PermissionKeys.Payments.Manage)]
    public async Task<ActionResult<PaymentTransactionDto>> Refund(
        Guid id,
        [FromBody] RefundPaymentRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.RefundPaymentAsync(id, request?.Reason, cancellationToken);
        return Ok(result);
    }
}

public sealed record RefundPaymentRequest(string? Reason);
