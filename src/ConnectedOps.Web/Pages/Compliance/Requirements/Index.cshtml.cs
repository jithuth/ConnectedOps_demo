using ConnectedOps.Application.Compliance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Compliance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Compliance.Requirements;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IComplianceRequirementService _requirementService;

    public IndexModel(IComplianceRequirementService requirementService)
    {
        _requirementService = requirementService;
    }

    [BindProperty(SupportsGet = true)]
    public ComplianceSubjectType? SubjectType { get; set; }

    [BindProperty(SupportsGet = true)]
    public ComplianceRequirementType? RequirementType { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsMandatory { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<ComplianceRequirementDto> Requirements { get; private set; } = null!;
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public CreateRequirementInput NewRequirement { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadRequirementsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NewRequirement.Code) || string.IsNullOrWhiteSpace(NewRequirement.Name))
        {
            ErrorMessage = "Requirement Code and Name are required.";
            await LoadRequirementsAsync(cancellationToken);
            return Page();
        }

        try
        {
            await _requirementService.CreateAsync(new CreateComplianceRequirementRequest(
                Code: NewRequirement.Code.Trim(),
                Name: NewRequirement.Name.Trim(),
                AppliesTo: NewRequirement.AppliesTo,
                RequirementType: NewRequirement.RequirementType,
                ValidityType: NewRequirement.ValidityType,
                Description: NewRequirement.Description,
                DefaultValidityDays: NewRequirement.DefaultValidityDays,
                DefaultReminderDays: NewRequirement.DefaultReminderDays,
                IsMandatory: NewRequirement.IsMandatory,
                IsActive: true), cancellationToken);

            SuccessMessage = $"Requirement '{NewRequirement.Name}' created successfully.";
            return RedirectToPage("/Compliance/Requirements/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadRequirementsAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _requirementService.DeleteAsync(id, cancellationToken);
            SuccessMessage = "Requirement deactivated/removed successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage("/Compliance/Requirements/Index");
    }

    private async Task LoadRequirementsAsync(CancellationToken cancellationToken)
    {
        var filter = new ComplianceRequirementFilterRequest(
            AppliesTo: SubjectType,
            RequirementType: RequirementType,
            IsMandatory: IsMandatory,
            IsActive: IsActive,
            SearchTerm: SearchTerm,
            Page: PageNumber,
            PageSize: 20);

        Requirements = await _requirementService.GetPagedAsync(filter, cancellationToken);
    }

    public sealed class CreateRequirementInput
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ComplianceSubjectType AppliesTo { get; set; } = ComplianceSubjectType.Vehicle;
        public ComplianceRequirementType RequirementType { get; set; } = ComplianceRequirementType.Registration;
        public ComplianceValidityType ValidityType { get; set; } = ComplianceValidityType.Recurring;
        public bool IsMandatory { get; set; } = true;
        public int? DefaultValidityDays { get; set; } = 365;
        public int DefaultReminderDays { get; set; } = 30;
    }
}
