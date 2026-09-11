using ConnectedOps.Application.Organization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization;

public sealed class HierarchyModel : PageModel
{
    private readonly IOrganizationHierarchyService _hierarchyService;

    public HierarchyModel(IOrganizationHierarchyService hierarchyService)
    {
        _hierarchyService = hierarchyService;
    }

    public IReadOnlyCollection<OrganizationHierarchyNodeDto> Hierarchy { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Hierarchy = await _hierarchyService.GetHierarchyAsync(HttpContext.RequestAborted);
    }
}
