using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Organization;

public sealed class OrganizationSettings : BaseEntity
{
    private OrganizationSettings()
    {
    }

    public OrganizationSettings(
        Guid tenantId,
        bool enforceBranchAssignment = false,
        bool enforceDepartmentAssignment = false,
        bool autoCreateEmployeeForUser = false,
        int fiscalYearStartMonth = 1,
        string? defaultWorkingDaysJson = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        Update(
            enforceBranchAssignment,
            enforceDepartmentAssignment,
            autoCreateEmployeeForUser,
            fiscalYearStartMonth,
            defaultWorkingDaysJson);
    }

    public Guid TenantId { get; private set; }
    public bool EnforceBranchAssignment { get; private set; }
    public bool EnforceDepartmentAssignment { get; private set; }
    public bool AutoCreateEmployeeForUser { get; private set; }
    public int FiscalYearStartMonth { get; private set; } = 1;
    public string? DefaultWorkingDaysJson { get; private set; }

    public void Update(
        bool enforceBranchAssignment,
        bool enforceDepartmentAssignment,
        bool autoCreateEmployeeForUser,
        int fiscalYearStartMonth,
        string? defaultWorkingDaysJson)
    {
        if (fiscalYearStartMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(fiscalYearStartMonth), "Fiscal year start month must be between 1 and 12.");

        EnforceBranchAssignment = enforceBranchAssignment;
        EnforceDepartmentAssignment = enforceDepartmentAssignment;
        AutoCreateEmployeeForUser = autoCreateEmployeeForUser;
        FiscalYearStartMonth = fiscalYearStartMonth;
        DefaultWorkingDaysJson = defaultWorkingDaysJson?.Trim();

        MarkUpdated();
    }
}
