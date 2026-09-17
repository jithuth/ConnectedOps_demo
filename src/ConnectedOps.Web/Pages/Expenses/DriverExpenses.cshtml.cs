using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Expenses;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Expenses;

[Authorize]
public class DriverExpensesModel : PageModel
{
    private readonly IDriverExpenseService _expenseService;
    private readonly IDriverService _driverService;
    private readonly IVehicleService _vehicleService;

    public DriverExpensesModel(
        IDriverExpenseService expenseService,
        IDriverService driverService,
        IVehicleService vehicleService)
    {
        _expenseService = expenseService;
        _driverService = driverService;
        _vehicleService = vehicleService;
    }

    public DriverExpenseDashboardDto Dashboard { get; private set; } = null!;
    public PagedResult<DriverTripExpenseDto> Expenses { get; private set; } = null!;
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? DriverIdFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public ExpenseStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public ExpenseCategory? CategoryFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _expenseService.GetDashboardAsync(cancellationToken);

        Expenses = await _expenseService.GetExpensesPagedAsync(
            new ExpenseFilterRequest(
                DriverId: DriverIdFilter,
                Status: StatusFilter,
                Category: CategoryFilter,
                PageNumber: PageNumber,
                PageSize: 20),
            cancellationToken);

        var driversPaged = await _driverService.GetDriversPagedAsync(
            new DriverQueryParameters { PageNumber = 1, PageSize = 100 },
            cancellationToken);
        Drivers = driversPaged.Items;

        var vehiclesPaged = await _vehicleService.GetVehiclesPagedAsync(
            new VehicleQueryParameters { PageNumber = 1, PageSize = 100 },
            cancellationToken);
        Vehicles = vehiclesPaged.Items;
    }

    public async Task<IActionResult> OnPostSubmitAsync(
        Guid driverId,
        ExpenseCategory category,
        decimal amount,
        string description,
        DateTime incurredAtUtc,
        Guid? vehicleId,
        string currency,
        CancellationToken cancellationToken)
    {
        try
        {
            await _expenseService.SubmitExpenseAsync(
                new SubmitDriverExpenseRequest(
                    DriverId: driverId,
                    Category: category,
                    Amount: amount,
                    Description: description,
                    IncurredAtUtc: incurredAtUtc,
                    VehicleId: vehicleId,
                    Currency: currency),
                cancellationToken);

            SuccessMessage = $"Expense for {amount} {currency} submitted successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseService.ApproveExpenseAsync(expenseId, new ApproveExpenseRequest(), cancellationToken);
            SuccessMessage = "Expense approved for reimbursement.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid expenseId, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseService.RejectExpenseAsync(expenseId, new RejectExpenseRequest(reason), cancellationToken);
            SuccessMessage = "Expense has been rejected.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReimburseAsync(Guid expenseId, string reference, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseService.ReimburseExpenseAsync(expenseId, new ReimburseExpenseRequest(reference), cancellationToken);
            SuccessMessage = $"Expense marked as reimbursed with payment ref: {reference}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
