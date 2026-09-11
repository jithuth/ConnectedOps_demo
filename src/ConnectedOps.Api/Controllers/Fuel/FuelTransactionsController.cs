using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Fuel;

[ApiController]
[Authorize]
public sealed class FuelTransactionsController : ControllerBase
{
    private readonly IFuelTransactionService _transactionService;
    private readonly IFuelImportService _importService;

    public FuelTransactionsController(
        IFuelTransactionService transactionService,
        IFuelImportService importService)
    {
        _transactionService = transactionService;
        _importService = importService;
    }

    [HttpGet("api/fuel/transactions")]
    [RequirePermission(PermissionKeys.FuelTransactions.View)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] FuelTransactionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetTransactionsPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/fuel/transactions/{id:guid}")]
    [RequirePermission(PermissionKeys.FuelTransactions.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/fuel/transactions")]
    [RequirePermission(PermissionKeys.FuelTransactions.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFuelTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("api/fuel/transactions/{id:guid}")]
    [RequirePermission(PermissionKeys.FuelTransactions.Edit)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFuelTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/fuel/transactions/{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.FuelTransactions.Cancel)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelFuelTransactionRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.CancelAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/fuel/transactions/{id:guid}/documents")]
    [RequirePermission(PermissionKeys.FuelDocuments.Manage)]
    public async Task<IActionResult> AddDocument(
        Guid id,
        [FromBody] AddFuelTransactionDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.AddDocumentAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/fuel/transactions/{id:guid}/documents/{documentId:guid}")]
    [RequirePermission(PermissionKeys.FuelDocuments.Manage)]
    public async Task<IActionResult> DeleteDocument(
        Guid id,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _transactionService.DeleteDocumentAsync(id, documentId, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/fuel/transactions/import")]
    [RequirePermission(PermissionKeys.FuelTransactions.Import)]
    public async Task<IActionResult> Import(
        [FromBody] ImportFuelTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _importService.ImportTransactionsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/fuel/imports/{batchId:guid}")]
    [RequirePermission(PermissionKeys.FuelTransactions.View)]
    public async Task<IActionResult> GetImportBatch(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var result = await _importService.GetBatchByIdAsync(batchId, cancellationToken);
        return Ok(result);
    }
}
