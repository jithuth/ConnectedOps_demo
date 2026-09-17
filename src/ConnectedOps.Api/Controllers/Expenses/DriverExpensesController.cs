using ConnectedOps.Application.Expenses;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Expenses;

[ApiController]
[Route("api/driver-expenses")]
[Authorize]
public sealed class DriverExpensesController : ControllerBase
{
    private readonly IDriverExpenseService _expenseService;

    public DriverExpensesController(IDriverExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.DriverExpenses.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.DriverExpenses.View)]
    public async Task<IActionResult> GetExpenses([FromQuery] ExpenseFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetExpensesPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.DriverExpenses.Submit)]
    public async Task<IActionResult> SubmitExpense([FromBody] SubmitDriverExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseService.SubmitExpenseAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetExpenses), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(PermissionKeys.DriverExpenses.Approve)]
    public async Task<IActionResult> ApproveExpense(Guid id, [FromBody] ApproveExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseService.ApproveExpenseAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(PermissionKeys.DriverExpenses.Approve)]
    public async Task<IActionResult> RejectExpense(Guid id, [FromBody] RejectExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseService.RejectExpenseAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reimburse")]
    [RequirePermission(PermissionKeys.DriverExpenses.Reimburse)]
    public async Task<IActionResult> ReimburseExpense(Guid id, [FromBody] ReimburseExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseService.ReimburseExpenseAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
