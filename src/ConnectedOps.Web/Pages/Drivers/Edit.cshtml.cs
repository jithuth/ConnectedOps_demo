using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Drivers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Drivers;

public sealed class EditModel : PageModel
{
    private readonly IDriverService _driverService;

    public EditModel(IDriverService driverService)
    {
        _driverService = driverService;
    }

    [BindProperty]
    public Guid Id { get; set; }

    public string DriverNumber { get; private set; } = string.Empty;

    [BindProperty]
    public UpdateDriverRequest Input { get; set; } = new(
        string.Empty,
        null,
        string.Empty,
        null,
        string.Empty,
        null,
        null,
        DriverType.Employee,
        null,
        null,
        null,
        null,
        null,
        null,
        null);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Id = id;
        try
        {
            var driver = await _driverService.GetDriverByIdAsync(id, HttpContext.RequestAborted);
            DriverNumber = driver.DriverNumber;
            Input = new UpdateDriverRequest(
                driver.FirstName,
                driver.MiddleName,
                driver.LastName,
                driver.DisplayName,
                driver.Phone,
                driver.AlternatePhone,
                driver.Email,
                driver.DriverType,
                driver.DateOfBirth,
                driver.NationalityCode,
                driver.PreferredLanguage,
                driver.HireDate,
                driver.StartDate,
                driver.EndDate,
                driver.Notes);

            return Page();
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = $"Driver '{id}' was not found.";
            return RedirectToPage("/Drivers/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await _driverService.UpdateDriverAsync(Id, Input, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Driver '{result.DriverNumber} - {result.DisplayName}' updated successfully.";
            return RedirectToPage("/Drivers/Details", new { id = Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return Page();
        }
    }
}
