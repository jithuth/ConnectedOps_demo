using ConnectedOps.Application.Compliance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Compliance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Compliance.Exceptions;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IComplianceExceptionService _exceptionService;
    private readonly IComplianceRequirementService _requirementService;

    public IndexModel(
        IComplianceExceptionService exceptionService,
        IComplianceRequirementService requirementService)
    {
        _exceptionService = exceptionService;
        _requirementService = requirementService;
    }

    [BindProperty(SupportsGet = true)]
    public ComplianceExceptionStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public ComplianceSubjectType? SubjectType { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? RequirementId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<ComplianceExceptionDto> Exceptions { get; private set; } = null!;
    public List<SelectListItem> RequirementsList { get; private set; } = [];

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public CreateExceptionInput NewException { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadDataAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (NewException.ComplianceRequirementId == Guid.Empty || NewException.SubjectId == Guid.Empty || string.IsNullOrWhiteSpace(NewException.Reason))
        {
            ErrorMessage = "Requirement, Subject ID, and Exemption Reason are required.";
            await LoadDataAsync(cancellationToken);
            return Page();
        }

        try
        {
            Guid? vehicleId = NewException.SubjectType == ComplianceSubjectType.Vehicle ? NewException.SubjectId : null;
            Guid? driverId = NewException.SubjectType == ComplianceSubjectType.Driver ? NewException.SubjectId : null;
            Guid? assetId = NewException.SubjectType == ComplianceSubjectType.Asset ? NewException.SubjectId : null;

            var effectiveTo = NewException.ValidToUtc ?? NewException.ValidFromUtc.AddDays(90);

            await _exceptionService.CreateAsync(new CreateComplianceExceptionRequest(
                ComplianceRequirementId: NewException.ComplianceRequirementId,
                SubjectType: NewException.SubjectType,
                Reason: NewException.Reason.Trim(),
                EffectiveFromUtc: NewException.ValidFromUtc,
                EffectiveToUtc: effectiveTo,
                VehicleId: vehicleId,
                DriverId: driverId,
                AssetId: assetId), cancellationToken);

            SuccessMessage = "Compliance exception requested successfully.";
            return RedirectToPage("/Compliance/Exceptions/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id, string? approvalNotes, CancellationToken cancellationToken)
    {
        try
        {
            await _exceptionService.ApproveAsync(id, new ApproveComplianceExceptionRequest(approvalNotes), cancellationToken);
            SuccessMessage = "Exception approved successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Compliance/Exceptions/Index");
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id, string? rejectionReason, CancellationToken cancellationToken)
    {
        try
        {
            await _exceptionService.RejectAsync(id, new RejectComplianceExceptionRequest(rejectionReason ?? "Rejected by compliance officer"), cancellationToken);
            SuccessMessage = "Exception rejected.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Compliance/Exceptions/Index");
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _exceptionService.CancelAsync(id, cancellationToken);
            SuccessMessage = "Exception cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Compliance/Exceptions/Index");
    }

    private async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        var reqs = await _requirementService.GetAllActiveAsync(cancellationToken);
        RequirementsList = reqs.Select(r => new SelectListItem($"{r.Code} - {r.Name} ({r.AppliesTo})", r.Id.ToString(), r.Id == RequirementId)).ToList();

        var filter = new ComplianceExceptionFilterRequest(
            ComplianceRequirementId: RequirementId,
            SubjectType: SubjectType,
            Status: Status,
            Page: PageNumber,
            PageSize: 20);

        Exceptions = await _exceptionService.GetPagedAsync(filter, cancellationToken);
    }

    public sealed class CreateExceptionInput
    {
        public Guid ComplianceRequirementId { get; set; }
        public ComplianceSubjectType SubjectType { get; set; } = ComplianceSubjectType.Vehicle;
        public Guid SubjectId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow.Date;
        public DateTime? ValidToUtc { get; set; } = DateTime.UtcNow.Date.AddDays(90);
        public string? SupportingDocumentReference { get; set; }
        public string? Notes { get; set; }
    }
}
