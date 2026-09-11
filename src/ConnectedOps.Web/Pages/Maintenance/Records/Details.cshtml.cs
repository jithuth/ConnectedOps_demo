using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance.Records;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IMaintenanceRecordService _recordService;

    public DetailsModel(IMaintenanceRecordService recordService)
    {
        _recordService = recordService;
    }

    public VehicleMaintenanceRecordDto Record { get; private set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Record = await _recordService.GetByIdAsync(id);
            return Page();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostStartAsync(Guid id)
    {
        try
        {
            await _recordService.StartServiceAsync(id);
            SuccessMessage = "Service started successfully. Vehicle status updated to UnderMaintenance.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCompleteAsync(
        Guid id,
        DateTime completedDateTimeUtc,
        decimal? finalOdometer,
        decimal? finalEngineHours,
        string? notes)
    {
        try
        {
            await _recordService.CompleteServiceAsync(id, new CompleteMaintenanceRecordRequest(
                completedDateTimeUtc,
                finalOdometer,
                finalEngineHours,
                notes));

            SuccessMessage = "Maintenance service completed successfully. Odometer and due schedules updated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, string? reason)
    {
        try
        {
            await _recordService.CancelServiceAsync(id, new CancelMaintenanceRecordRequest(reason));
            SuccessMessage = "Maintenance service cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    // Tasks
    public async Task<IActionResult> OnPostAddTaskAsync(Guid id, string name, string? description, string? notes)
    {
        try
        {
            await _recordService.AddTaskAsync(id, new AddMaintenanceTaskRequest(name, null, description, notes));
            SuccessMessage = "Maintenance task added.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateTaskStatusAsync(Guid id, Guid taskId, MaintenanceTaskStatus status, string? notes)
    {
        try
        {
            await _recordService.UpdateTaskStatusAsync(id, taskId, new UpdateMaintenanceTaskStatusRequest(status, notes));
            SuccessMessage = "Task status updated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteTaskAsync(Guid id, Guid taskId)
    {
        try
        {
            await _recordService.DeleteTaskAsync(id, taskId);
            SuccessMessage = "Task removed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    // Parts
    public async Task<IActionResult> OnPostAddPartAsync(
        Guid id,
        string partName,
        decimal quantity,
        string? partNumber,
        string unit,
        decimal? unitCost,
        string? supplier,
        string? notes)
    {
        try
        {
            await _recordService.AddPartAsync(id, new AddMaintenancePartRequest(partName, quantity, partNumber, unit, unitCost, supplier, notes));
            SuccessMessage = "Part added and total cost recalculated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeletePartAsync(Guid id, Guid partId)
    {
        try
        {
            await _recordService.DeletePartAsync(id, partId);
            SuccessMessage = "Part removed and totals recalculated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    // Labour
    public async Task<IActionResult> OnPostAddLabourAsync(
        Guid id,
        string description,
        decimal hours,
        decimal? hourlyRate,
        string? technicianName,
        string? notes)
    {
        try
        {
            await _recordService.AddLabourAsync(id, new AddMaintenanceLabourRequest(description, hours, hourlyRate, technicianName, null, notes));
            SuccessMessage = "Labour entry added and total cost recalculated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteLabourAsync(Guid id, Guid labourId)
    {
        try
        {
            await _recordService.DeleteLabourAsync(id, labourId);
            SuccessMessage = "Labour entry removed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    // Expenses
    public async Task<IActionResult> OnPostAddExpenseAsync(
        Guid id,
        MaintenanceExpenseType expenseType,
        string description,
        decimal amount,
        string? reference,
        string? notes)
    {
        try
        {
            await _recordService.AddExpenseAsync(id, new AddMaintenanceExpenseRequest(expenseType, description, amount, "USD", reference, notes));
            SuccessMessage = "Expense entry added.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteExpenseAsync(Guid id, Guid expenseId)
    {
        try
        {
            await _recordService.DeleteExpenseAsync(id, expenseId);
            SuccessMessage = "Expense entry removed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    // Documents
    public async Task<IActionResult> OnPostAddDocumentAsync(
        Guid id,
        MaintenanceDocumentType documentType,
        string title,
        string fileObjectKey,
        string fileName,
        string? notes)
    {
        try
        {
            await _recordService.AddDocumentAsync(id, new AddMaintenanceDocumentRequest(documentType, title, fileObjectKey, fileName, null, null, notes));
            SuccessMessage = "Document attached.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteDocumentAsync(Guid id, Guid documentId)
    {
        try
        {
            await _recordService.DeleteDocumentAsync(id, documentId);
            SuccessMessage = "Document removed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }
}
