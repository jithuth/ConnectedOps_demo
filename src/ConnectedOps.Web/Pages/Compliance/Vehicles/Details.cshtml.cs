using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Compliance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Compliance.Vehicles;

[Authorize]
public sealed class DetailsModel : PageModel
{
    private readonly IComplianceEvaluationService _evaluationService;
    private readonly IComplianceRecordService _recordService;
    private readonly IComplianceRequirementService _requirementService;

    public DetailsModel(
        IComplianceEvaluationService evaluationService,
        IComplianceRecordService recordService,
        IComplianceRequirementService requirementService)
    {
        _evaluationService = evaluationService;
        _recordService = recordService;
        _requirementService = requirementService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ComplianceEvaluationResultDto Evaluation { get; private set; } = null!;
    public IReadOnlyList<ComplianceRecordDto> Records { get; private set; } = [];
    public List<SelectListItem> VehicleRequirements { get; private set; } = [];

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public AddRecordInput NewRecord { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Id == Guid.Empty)
            return RedirectToPage("/Compliance/Index");

        await LoadDataAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAddRecordAsync(CancellationToken cancellationToken)
    {
        if (NewRecord.ComplianceRequirementId == Guid.Empty)
        {
            ErrorMessage = "Please select a compliance requirement.";
            await LoadDataAsync(cancellationToken);
            return Page();
        }

        try
        {
            var notes = string.IsNullOrWhiteSpace(NewRecord.IssuingAuthority)
                ? NewRecord.Notes
                : (string.IsNullOrWhiteSpace(NewRecord.Notes) ? $"Authority: {NewRecord.IssuingAuthority}" : $"Authority: {NewRecord.IssuingAuthority}. {NewRecord.Notes}");

            await _recordService.CreateAsync(new CreateComplianceRecordRequest(
                ComplianceRequirementId: NewRecord.ComplianceRequirementId,
                SubjectType: ComplianceSubjectType.Vehicle,
                VehicleId: Id,
                IssueDateUtc: NewRecord.IssueDateUtc,
                ExpiryDateUtc: NewRecord.ExpiryDateUtc,
                ReferenceNumber: NewRecord.ReferenceNumber,
                Notes: notes), cancellationToken);

            SuccessMessage = "Compliance record added successfully.";
            return RedirectToPage("/Compliance/Vehicles/Details", new { id = Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostVerifyRecordAsync(Guid recordId, string? notes, CancellationToken cancellationToken)
    {
        try
        {
            await _recordService.VerifyAsync(recordId, new VerifyComplianceRecordRequest(Notes: notes), cancellationToken);
            SuccessMessage = "Compliance record verified successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Compliance/Vehicles/Details", new { id = Id });
    }

    private async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        Evaluation = await _evaluationService.EvaluateVehicleAsync(Id, cancellationToken);
        Records = await _recordService.GetSubjectRecordsAsync(ComplianceSubjectType.Vehicle, Id, cancellationToken);

        var activeReqs = await _requirementService.GetAllActiveAsync(cancellationToken);
        VehicleRequirements = activeReqs
            .Where(r => r.AppliesTo == ComplianceSubjectType.Vehicle)
            .Select(r => new SelectListItem($"{r.Code} - {r.Name}", r.Id.ToString()))
            .ToList();
    }

    public sealed class AddRecordInput
    {
        public Guid ComplianceRequirementId { get; set; }
        public DateTime? IssueDateUtc { get; set; } = DateTime.UtcNow.Date;
        public DateTime? ExpiryDateUtc { get; set; } = DateTime.UtcNow.Date.AddYears(1);
        public string? ReferenceNumber { get; set; }
        public string? IssuingAuthority { get; set; }
        public string? Notes { get; set; }
    }
}
