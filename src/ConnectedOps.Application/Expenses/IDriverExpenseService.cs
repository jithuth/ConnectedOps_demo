using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Expenses;

public interface IDriverExpenseService
{
    Task<DriverExpenseDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<DriverTripExpenseDto>> GetExpensesPagedAsync(ExpenseFilterRequest request, CancellationToken cancellationToken = default);

    Task<DriverTripExpenseDto> SubmitExpenseAsync(SubmitDriverExpenseRequest request, CancellationToken cancellationToken = default);

    Task<DriverTripExpenseDto> ApproveExpenseAsync(Guid expenseId, ApproveExpenseRequest request, CancellationToken cancellationToken = default);

    Task<DriverTripExpenseDto> RejectExpenseAsync(Guid expenseId, RejectExpenseRequest request, CancellationToken cancellationToken = default);

    Task<DriverTripExpenseDto> ReimburseExpenseAsync(Guid expenseId, ReimburseExpenseRequest request, CancellationToken cancellationToken = default);
}
