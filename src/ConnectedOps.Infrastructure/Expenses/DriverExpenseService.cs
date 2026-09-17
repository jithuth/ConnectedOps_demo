using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Expenses;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Expenses;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Expenses;

public sealed class DriverExpenseService : IDriverExpenseService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<DriverExpenseService> _logger;

    public DriverExpenseService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ILogger<DriverExpenseService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required for expense operations.");
    }

    public async Task<DriverExpenseDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var expenses = await _dbContext.DriverTripExpenses
            .AsNoTracking()
            .Include(e => e.Driver)
            .Include(e => e.Vehicle)
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var pending = expenses.Where(e => e.Status == ExpenseStatus.Submitted).ToList();
        var approved = expenses.Where(e => e.Status == ExpenseStatus.Approved).ToList();
        var reimbursedThisMonth = expenses.Where(e => e.Status == ExpenseStatus.Reimbursed && e.IncurredAtUtc >= startOfMonth).ToList();

        var recentExpenses = expenses
            .OrderByDescending(e => e.IncurredAtUtc)
            .Take(10)
            .Select(e => MapToDto(e, null))
            .ToList();

        return new DriverExpenseDashboardDto(
            TotalPendingApprovalAmount: pending.Sum(e => e.Amount),
            PendingApprovalCount: pending.Count,
            TotalApprovedPendingPayout: approved.Sum(e => e.Amount),
            ApprovedCount: approved.Count,
            TotalReimbursedThisMonth: reimbursedThisMonth.Sum(e => e.Amount),
            ReimbursedCount: reimbursedThisMonth.Count,
            RecentExpenses: recentExpenses);
    }

    public async Task<PagedResult<DriverTripExpenseDto>> GetExpensesPagedAsync(ExpenseFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.DriverTripExpenses
            .AsNoTracking()
            .Include(e => e.Driver)
            .Include(e => e.Vehicle)
            .Where(e => e.TenantId == tenantId);

        if (request.DriverId.HasValue)
        {
            query = query.Where(e => e.DriverId == request.DriverId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        if (request.Category.HasValue)
        {
            query = query.Where(e => e.Category == request.Category.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.IncurredAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => MapToDto(e))
            .ToListAsync(cancellationToken);

        return new PagedResult<DriverTripExpenseDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<DriverTripExpenseDto> SubmitExpenseAsync(SubmitDriverExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == request.DriverId, cancellationToken)
            ?? throw new KeyNotFoundException($"Driver {request.DriverId} was not found.");

        var expense = new DriverTripExpense(
            tenantId,
            request.DriverId,
            request.Category,
            request.Amount,
            request.Description,
            request.IncurredAtUtc,
            request.VehicleId,
            request.DispatchJobId,
            request.Currency,
            request.ReceiptImageKey);

        _dbContext.DriverTripExpenses.Add(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Driver {DriverId} submitted expense of {Amount} {Currency} under {Category}",
            request.DriverId, request.Amount, request.Currency, request.Category);

        return MapToDto(expense, driver.FirstName + " " + driver.LastName);
    }

    public async Task<DriverTripExpenseDto> ApproveExpenseAsync(Guid expenseId, ApproveExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var approverUserId = _currentUserContext.UserId ?? Guid.Empty;

        var expense = await _dbContext.DriverTripExpenses
            .Include(e => e.Driver)
            .Include(e => e.Vehicle)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == expenseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Expense {expenseId} was not found.");

        expense.Approve(approverUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expense {ExpenseId} approved by user {UserId}", expenseId, approverUserId);

        return MapToDto(expense);
    }

    public async Task<DriverTripExpenseDto> RejectExpenseAsync(Guid expenseId, RejectExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var rejectorUserId = _currentUserContext.UserId ?? Guid.Empty;

        var expense = await _dbContext.DriverTripExpenses
            .Include(e => e.Driver)
            .Include(e => e.Vehicle)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == expenseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Expense {expenseId} was not found.");

        expense.Reject(rejectorUserId, request.Reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expense {ExpenseId} rejected by user {UserId}: {Reason}", expenseId, rejectorUserId, request.Reason);

        return MapToDto(expense);
    }

    public async Task<DriverTripExpenseDto> ReimburseExpenseAsync(Guid expenseId, ReimburseExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var expense = await _dbContext.DriverTripExpenses
            .Include(e => e.Driver)
            .Include(e => e.Vehicle)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == expenseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Expense {expenseId} was not found.");

        expense.MarkReimbursed(request.Reference);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expense {ExpenseId} marked as reimbursed with ref {Reference}", expenseId, request.Reference);

        return MapToDto(expense);
    }

    private static DriverTripExpenseDto MapToDto(DriverTripExpense e, string? explicitDriverName = null)
    {
        var driverName = explicitDriverName
            ?? (e.Driver != null ? $"{e.Driver.FirstName} {e.Driver.LastName}".Trim() : "Unknown Driver");

        var vehiclePlate = e.Vehicle?.RegistrationNumber;

        return new DriverTripExpenseDto(
            e.Id,
            e.DriverId,
            driverName,
            e.VehicleId,
            vehiclePlate,
            e.DispatchJobId,
            e.Category,
            e.Category.ToString(),
            e.Amount,
            e.Currency,
            e.Description,
            e.ReceiptImageKey,
            e.IncurredAtUtc,
            e.Status,
            e.Status.ToString(),
            e.ApprovedByUserId,
            e.ApprovedAtUtc,
            e.RejectionReason,
            e.ReimbursementReference);
    }
}
