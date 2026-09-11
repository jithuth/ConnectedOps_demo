using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Drivers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Drivers;

public sealed class DetailsModel : PageModel
{
    private readonly IDriverService _driverService;
    private readonly IDriverLicenseService _licenseService;
    private readonly IDriverCertificationService _certificationService;
    private readonly IDriverDocumentService _documentService;
    private readonly IDriverAssignmentService _assignmentService;
    private readonly IBranchService _branchService;
    private readonly IDepartmentService _departmentService;

    public DetailsModel(
        IDriverService driverService,
        IDriverLicenseService licenseService,
        IDriverCertificationService certificationService,
        IDriverDocumentService documentService,
        IDriverAssignmentService assignmentService,
        IBranchService branchService,
        IDepartmentService departmentService)
    {
        _driverService = driverService;
        _licenseService = licenseService;
        _certificationService = certificationService;
        _documentService = documentService;
        _assignmentService = assignmentService;
        _branchService = branchService;
        _departmentService = departmentService;
    }

    public Guid DriverId { get; private set; }
    public DriverDetailDto Driver { get; private set; } = null!;
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<DepartmentListItemDto> Departments { get; private set; } = [];

    [BindProperty]
    public CreateDriverLicenseRequest LicenseInput { get; set; } = new(
        string.Empty,
        "US",
        null,
        null,
        null,
        true);

    [BindProperty]
    public CreateDriverCertificationRequest CertInput { get; set; } = new(
        CertificationType.DefensiveDriving,
        string.Empty,
        null,
        null,
        null,
        null,
        null,
        null);

    [BindProperty]
    public CreateDriverDocumentRequest DocInput { get; set; } = new(
        DriverDocumentType.MedicalFitness,
        string.Empty,
        null,
        null,
        null,
        null,
        "docs/driver_doc.pdf",
        "driver_doc.pdf",
        "application/pdf",
        1024,
        null);

    [BindProperty]
    public CreateDriverEmergencyContactRequest ContactInput { get; set; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        null,
        true);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        DriverId = id;
        try
        {
            var ct = HttpContext.RequestAborted;
            Driver = await _driverService.GetDriverByIdAsync(id, ct);
            Branches = await _branchService.GetBranchesAsync(ct);
            Departments = await _departmentService.GetDepartmentsAsync(cancellationToken: ct);
            return Page();
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = $"Driver '{id}' was not found.";
            return RedirectToPage("/Drivers/Index");
        }
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(Guid id, DriverStatus newStatus, string? notes)
    {
        try
        {
            await _driverService.ChangeStatusAsync(id, new ChangeDriverStatusRequest(newStatus, notes), HttpContext.RequestAborted);
            TempData["Feedback"] = "Driver status updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignBranchAsync(Guid id, Guid? branchId)
    {
        try
        {
            await _driverService.AssignBranchAsync(id, new AssignDriverBranchRequest(branchId), HttpContext.RequestAborted);
            TempData["Feedback"] = "Branch assignment updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignDepartmentAsync(Guid id, Guid? departmentId)
    {
        try
        {
            await _driverService.AssignDepartmentAsync(id, new AssignDriverDepartmentRequest(departmentId), HttpContext.RequestAborted);
            TempData["Feedback"] = "Department assignment updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddNoteAsync(Guid id, string noteText)
    {
        try
        {
            await _driverService.AddNoteAsync(id, new CreateDriverNoteRequest(noteText), HttpContext.RequestAborted);
            TempData["Feedback"] = "Note added successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddEmergencyContactAsync(Guid id)
    {
        try
        {
            await _driverService.AddEmergencyContactAsync(id, ContactInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Emergency contact '{ContactInput.Name}' added successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteEmergencyContactAsync(Guid id, Guid contactId)
    {
        try
        {
            await _driverService.DeleteEmergencyContactAsync(id, contactId, HttpContext.RequestAborted);
            TempData["Feedback"] = "Emergency contact removed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddLicenseAsync(Guid id)
    {
        try
        {
            await _licenseService.AddLicenseAsync(id, LicenseInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"License '{LicenseInput.LicenseNumber}' recorded.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSetPrimaryLicenseAsync(Guid id, Guid licenseId)
    {
        try
        {
            await _licenseService.SetPrimaryLicenseAsync(id, licenseId, HttpContext.RequestAborted);
            TempData["Feedback"] = "Primary license updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeactivateLicenseAsync(Guid id, Guid licenseId)
    {
        try
        {
            await _licenseService.DeactivateLicenseAsync(id, licenseId, HttpContext.RequestAborted);
            TempData["Feedback"] = "License deactivated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddLicenseCategoryAsync(Guid id, Guid licenseId, string categoryCode, string? description)
    {
        try
        {
            await _licenseService.AddCategoryAsync(id, licenseId, new AddDriverLicenseCategoryRequest(categoryCode, description), HttpContext.RequestAborted);
            TempData["Feedback"] = $"Category '{categoryCode}' added to license.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddCertificationAsync(Guid id)
    {
        try
        {
            await _certificationService.AddCertificationAsync(id, CertInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Certification '{CertInput.Title}' recorded.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeactivateCertificationAsync(Guid id, Guid certId)
    {
        try
        {
            await _certificationService.DeactivateCertificationAsync(id, certId, HttpContext.RequestAborted);
            TempData["Feedback"] = "Certification deactivated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddDocumentAsync(Guid id)
    {
        try
        {
            await _documentService.AddDocumentAsync(id, DocInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Document '{DocInput.Title}' added.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeactivateDocumentAsync(Guid id, Guid docId)
    {
        try
        {
            await _documentService.DeactivateDocumentAsync(id, docId, HttpContext.RequestAborted);
            TempData["Feedback"] = "Document deactivated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostEndAssignmentAsync(Guid id, Guid assignmentId, string? notes)
    {
        try
        {
            await _assignmentService.EndAssignmentAsync(
                assignmentId,
                new EndDriverVehicleAssignmentRequest(notes, DateTime.UtcNow),
                HttpContext.RequestAborted);

            TempData["Feedback"] = "Vehicle assignment ended successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }
}
