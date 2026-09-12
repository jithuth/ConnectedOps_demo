using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Safety;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Safety;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Safety.CorrectiveActions;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly ICorrectiveActionService _actionService;
    private readonly IEmployeeService _employeeService;

    public IndexModel(
        ICorrectiveActionService actionService,
        IEmployeeService employeeService)
    {
        _actionService = actionService;
        _employeeService = employeeService;
    }

    [BindProperty(SupportsGet = true)]
    public CorrectiveActionStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public CorrectiveActionPriority? Priority { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? OverdueOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? AssignedEmployeeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<CorrectiveActionDto> Actions { get; private set; } = null!;
    public List<SelectListItem> EmployeesList { get; private set; } = [];

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadDataAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string title,
        string description,
        CorrectiveActionPriority priority,
        DateTime? dueDateUtc,
        Guid? assignedEmployeeId,
        bool verificationRequired,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
        {
            ErrorMessage = "Title and Description are required.";
            await LoadDataAsync(cancellationToken);
            return Page();
        }

        try
        {
            await _actionService.CreateAsync(new CreateCorrectiveActionRequest(
                Title: title.Trim(),
                Description: description.Trim(),
                Priority: priority,
                DueDateUtc: dueDateUtc,
                AssignedEmployeeId: assignedEmployeeId,
                VerificationRequired: verificationRequired), cancellationToken);

            SuccessMessage = "Corrective action assigned successfully.";
            return RedirectToPage("/Safety/CorrectiveActions/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostStartAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _actionService.StartAsync(id, cancellationToken);
            SuccessMessage = "Action in progress.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/CorrectiveActions/Index");
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id, string? resolutionNotes, CancellationToken cancellationToken)
    {
        try
        {
            await _actionService.CompleteAsync(id, new CompleteCorrectiveActionRequest(resolutionNotes), cancellationToken);
            SuccessMessage = "Action completed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/CorrectiveActions/Index");
    }

    public async Task<IActionResult> OnPostVerifyAsync(Guid id, string? verificationNotes, CancellationToken cancellationToken)
    {
        try
        {
            await _actionService.VerifyAsync(id, new VerifyCorrectiveActionRequest(verificationNotes), cancellationToken);
            SuccessMessage = "Corrective action verified.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Safety/CorrectiveActions/Index");
    }

    private async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        var filter = new CorrectiveActionFilterRequest(
            Status: Status,
            Priority: Priority,
            AssignedEmployeeId: AssignedEmployeeId,
            OverdueOnly: OverdueOnly,
            SearchTerm: SearchTerm,
            Page: PageNumber,
            PageSize: 20);

        Actions = await _actionService.GetPagedAsync(filter, cancellationToken);

        var employees = await _employeeService.GetEmployeesAsync(cancellationToken: cancellationToken);
        EmployeesList = employees.Select(e => new SelectListItem($"{e.FullName} ({e.JobTitle ?? "Staff"})", e.Id.ToString(), e.Id == AssignedEmployeeId)).ToList();
    }
}
