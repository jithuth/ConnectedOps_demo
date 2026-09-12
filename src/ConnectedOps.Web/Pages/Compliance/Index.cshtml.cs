using ConnectedOps.Application.Compliance;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Compliance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Compliance;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IComplianceEvaluationService _evaluationService;
    private readonly IBranchService _branchService;

    public IndexModel(
        IComplianceEvaluationService evaluationService,
        IBranchService branchService)
    {
        _evaluationService = evaluationService;
        _branchService = branchService;
    }

    [BindProperty(SupportsGet = true)]
    public ComplianceSubjectType? SubjectType { get; set; }

    [BindProperty(SupportsGet = true)]
    public ComplianceStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? BranchId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public IReadOnlyList<SubjectComplianceSummaryDto> Subjects { get; private set; } = [];
    public List<SelectListItem> Branches { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var branches = await _branchService.GetBranchesAsync(cancellationToken);
        Branches = branches.Select(b => new SelectListItem(b.Name, b.Id.ToString(), b.Id == BranchId)).ToList();

        var allSubjects = await _evaluationService.EvaluateAllSubjectsAsync(SubjectType, BranchId, cancellationToken);

        if (Status.HasValue)
        {
            allSubjects = allSubjects.Where(s => s.OverallStatus == Status.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            allSubjects = allSubjects.Where(s =>
                s.SubjectDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.SubjectIdentifier.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (s.BranchName != null && s.BranchName.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        Subjects = allSubjects;
    }
}
