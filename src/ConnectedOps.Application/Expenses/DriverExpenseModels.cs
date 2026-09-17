using ConnectedOps.Domain.Expenses;

namespace ConnectedOps.Application.Expenses;

public sealed record DriverTripExpenseDto(
    Guid Id,
    Guid DriverId,
    string DriverName,
    Guid? VehicleId,
    string? VehiclePlateNumber,
    Guid? DispatchJobId,
    ExpenseCategory Category,
    string CategoryName,
    decimal Amount,
    string Currency,
    string Description,
    string? ReceiptImageKey,
    DateTime IncurredAtUtc,
    ExpenseStatus Status,
    string StatusName,
    Guid? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? RejectionReason,
    string? ReimbursementReference);

public sealed record SubmitDriverExpenseRequest(
    Guid DriverId,
    ExpenseCategory Category,
    decimal Amount,
    string Description,
    DateTime IncurredAtUtc,
    Guid? VehicleId = null,
    Guid? DispatchJobId = null,
    string Currency = "AED",
    string? ReceiptImageKey = null);

public sealed record ApproveExpenseRequest(
    string? Notes = null);

public sealed record RejectExpenseRequest(
    string Reason);

public sealed record ReimburseExpenseRequest(
    string Reference);

public sealed record ExpenseFilterRequest(
    Guid? DriverId = null,
    ExpenseStatus? Status = null,
    ExpenseCategory? Category = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record DriverExpenseDashboardDto(
    decimal TotalPendingApprovalAmount,
    int PendingApprovalCount,
    decimal TotalApprovedPendingPayout,
    int ApprovedCount,
    decimal TotalReimbursedThisMonth,
    int ReimbursedCount,
    List<DriverTripExpenseDto> RecentExpenses);
