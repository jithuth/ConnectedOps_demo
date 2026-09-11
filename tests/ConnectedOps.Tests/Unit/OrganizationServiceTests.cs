using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Organization;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class OrganizationServiceTests
{
    [Fact]
    public async Task OrganizationProfileService_CreatesAndUpdatesProfile_ForCurrentTenant()
    {
        using var context = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var tenant = new Tenant("Acme Fleet Corp", "ACME", "contact@acme.com");
        typeof(Tenant).GetProperty("Id")!.SetValue(tenant, tenantId);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var userContext = new TestUserContext
        {
            UserId = userId,
            TenantId = tenantId
        };
        var auditService = new TestAuditLogService();
        var profileService = new OrganizationProfileService(context, userContext, auditService);

        // Act - Get should auto-initialize
        var initial = await profileService.GetProfileAsync();
        Assert.NotNull(initial);
        Assert.Equal("Acme Fleet Corp", initial.LegalName);
        Assert.Equal(tenantId, initial.TenantId);

        // Act - Update
        var updated = await profileService.UpdateProfileAsync(new UpdateOrganizationProfileRequest(
            LegalName: "Acme Global Fleet Logistics Ltd.",
            TradeName: "Acme Logistics",
            RegistrationNumber: "REG-12345",
            TaxNumber: "TAX-67890",
            Website: "https://acme.com",
            PrimaryContactEmail: "ops@acme.com",
            PrimaryContactPhone: "+1 555-0100",
            AddressLine1: "100 Logistics Blvd",
            AddressLine2: "Suite 400",
            City: "Chicago",
            StateOrProvince: "IL",
            PostalCode: "60601",
            CountryCode: "US",
            CurrencyCode: "USD",
            TimeZoneId: "Central Standard Time"));

        Assert.Equal("Acme Global Fleet Logistics Ltd.", updated.LegalName);
        Assert.Equal("Acme Logistics", updated.TradeName);
        Assert.Equal("Chicago", updated.City);
        Assert.Single(auditService.WrittenLogs);
    }

    [Fact]
    public async Task BranchService_EnforcesTenantIsolation_TenantACannotSeeTenantBBranches()
    {
        using var context = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var branchA = new Branch(tenantA, "Depot Alpha", "BR-ALPHA");
        var branchB = new Branch(tenantB, "Depot Beta", "BR-BETA");

        context.Branches.AddRange(branchA, branchB);
        await context.SaveChangesAsync();

        var userContextA = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantA };
        var auditService = new TestAuditLogService();
        var branchServiceA = new BranchService(context, userContextA, auditService);

        var branchesForA = await branchServiceA.GetBranchesAsync();

        Assert.Single(branchesForA);
        Assert.Equal("Depot Alpha", branchesForA.First().Name);

        // Tenant A cannot get Tenant B's branch by ID
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            branchServiceA.GetBranchByIdAsync(branchB.Id));
    }

    [Fact]
    public async Task BranchService_DetectsDuplicateBranchCode_ThrowsConflictException()
    {
        using var context = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var existingBranch = new Branch(tenantId, "Depot One", "BR-01");
        context.Branches.Add(existingBranch);
        await context.SaveChangesAsync();

        var userContext = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantId };
        var branchService = new BranchService(context, userContext, new TestAuditLogService());

        var req = new CreateBranchRequest(
            Name: "Duplicate Depot",
            Code: "br-01", // case-insensitive duplicate
            Type: BranchType.Depot,
            IsHeadOffice: false,
            Email: null,
            Phone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            StateOrProvince: null,
            PostalCode: null,
            CountryCode: null,
            TimeZoneId: null);

        await Assert.ThrowsAsync<ConflictException>(() => branchService.CreateBranchAsync(req));
    }

    [Fact]
    public async Task BranchService_RejectsMultipleHeadOffices_ThrowsConflictException()
    {
        using var context = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var existingHQ = new Branch(tenantId, "HQ Hub", "BR-HQ", BranchType.HeadOffice, isHeadOffice: true);
        context.Branches.Add(existingHQ);
        await context.SaveChangesAsync();

        var userContext = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantId };
        var branchService = new BranchService(context, userContext, new TestAuditLogService());

        var req = new CreateBranchRequest(
            Name: "Second HQ",
            Code: "BR-HQ2",
            Type: BranchType.HeadOffice,
            IsHeadOffice: true,
            Email: null,
            Phone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            StateOrProvince: null,
            PostalCode: null,
            CountryCode: null,
            TimeZoneId: null);

        await Assert.ThrowsAsync<ConflictException>(() => branchService.CreateBranchAsync(req));
    }

    [Fact]
    public async Task LocationService_RejectsBranch_FromDifferentTenant()
    {
        using var context = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var branchB = new Branch(tenantB, "Branch Beta", "BR-BETA");
        context.Branches.Add(branchB);
        await context.SaveChangesAsync();

        var userContextA = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantA };
        var locationServiceA = new LocationService(context, userContextA, new TestAuditLogService());

        var req = new CreateLocationRequest(
            BranchId: branchB.Id, // Branch belongs to Tenant B!
            Name: "Illegal Location",
            Code: "LOC-ILL",
            Type: LocationType.Depot,
            Latitude: 40.7,
            Longitude: -74.0,
            GeofenceRadiusMeters: 100,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            StateOrProvince: null,
            PostalCode: null,
            CountryCode: null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => locationServiceA.CreateLocationAsync(req));
    }

    [Fact]
    public async Task DepartmentService_PreventsSelfParenting_ThrowsInvalidOperationException()
    {
        using var context = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var dept = new Department(tenantId, "Operations", "DEP-OPS");
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var userContext = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantId };
        var deptService = new DepartmentService(context, userContext, new TestAuditLogService());

        var updateReq = new UpdateDepartmentRequest(
            BranchId: null,
            ParentDepartmentId: dept.Id, // Self-parenting!
            Name: "Operations",
            Code: "DEP-OPS",
            Description: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => deptService.UpdateDepartmentAsync(dept.Id, updateReq));
    }

    [Fact]
    public async Task EmployeeService_EnforcesUniqueEmployeeNumber_WithinTenant()
    {
        using var context = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var emp1 = new Employee(tenantId, "EMP-001", "John", "Doe", "john@company.com");
        context.Employees.Add(emp1);
        await context.SaveChangesAsync();

        var userContext = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantId };
        var employeeService = new EmployeeService(context, userContext, new TestAuditLogService());

        var req = new CreateEmployeeRequest(
            EmployeeNumber: "emp-001", // Duplicate
            FirstName: "Jane",
            LastName: "Smith",
            Email: "jane@company.com",
            Phone: null,
            JobTitle: null,
            EmploymentType: EmploymentType.FullTime,
            EmploymentStatus: EmploymentStatus.Active,
            BranchId: null,
            DepartmentId: null,
            TeamId: null,
            ManagerEmployeeId: null,
            UserId: null,
            HireDate: null);

        await Assert.ThrowsAsync<ConflictException>(() => employeeService.CreateEmployeeAsync(req));
    }

    [Fact]
    public async Task EmployeeService_LinksAndUnlinksUser_Successfully()
    {
        using var context = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var tenantUser = new TenantUser(tenantId, userId);
        var emp = new Employee(tenantId, "EMP-010", "Michael", "Scott", "michael@dundermifflin.com");

        context.TenantUsers.Add(tenantUser);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var userContext = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantId };
        var auditService = new TestAuditLogService();
        var employeeService = new EmployeeService(context, userContext, auditService);

        // Act - Link
        await employeeService.LinkUserAsync(emp.Id, userId);

        var linkedEmp = await employeeService.GetEmployeeByIdAsync(emp.Id);
        Assert.Equal(userId, linkedEmp.UserId);

        // Act - Unlink
        await employeeService.UnlinkUserAsync(emp.Id);

        var unlinkedEmp = await employeeService.GetEmployeeByIdAsync(emp.Id);
        Assert.Null(unlinkedEmp.UserId);
    }

    [Fact]
    public async Task OrganizationHierarchyService_BuildsCorrectNestedTree()
    {
        using var context = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant("Nexus Fleet Inc", "NEXUS");
        typeof(Tenant).GetProperty("Id")!.SetValue(tenant, tenantId);
        context.Tenants.Add(tenant);

        var branch = new Branch(tenantId, "Chicago Branch", "BR-ORD", BranchType.HeadOffice, isHeadOffice: true);
        context.Branches.Add(branch);

        var dept = new Department(tenantId, "Fleet Dispatch", "DEP-DISP", branchId: branch.Id);
        context.Departments.Add(dept);

        var team = new Team(tenantId, dept.Id, "Team Bravo", "TM-BRAVO");
        context.Teams.Add(team);

        var emp = new Employee(tenantId, "EMP-900", "Carlos", "Ray", "carlos@nexus.com", branchId: branch.Id, departmentId: dept.Id, teamId: team.Id);
        context.Employees.Add(emp);

        await context.SaveChangesAsync();

        var userContext = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantId };
        var hierarchyService = new OrganizationHierarchyService(context, userContext);

        var hierarchy = await hierarchyService.GetHierarchyAsync();

        Assert.Single(hierarchy);
        var root = hierarchy.First();
        Assert.Equal("Organization", root.NodeType);
        Assert.Equal("Nexus Fleet Inc", root.Name);

        Assert.Single(root.Children);
        var branchNode = root.Children.First();
        Assert.Equal("Branch", branchNode.NodeType);

        var deptNode = branchNode.Children.First(c => c.NodeType == "Department");
        Assert.Equal("Fleet Dispatch", deptNode.Name);

        var teamNode = deptNode.Children.First(c => c.NodeType == "Team");
        Assert.Equal("Team Bravo", teamNode.Name);

        var empNode = teamNode.Children.First();
        Assert.Equal("Employee", empNode.NodeType);
        Assert.Equal("Carlos Ray", empNode.Name);
    }

    [Fact]
    public async Task OrganizationDashboardService_ComputesCorrectMetrics()
    {
        using var context = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();

        var b1 = new Branch(tenantId, "Depot 1", "D1");
        var b2 = new Branch(tenantId, "Depot 2", "D2");
        b2.Deactivate();

        var dept = new Department(tenantId, "Logistics", "LOG");
        var team = new Team(tenantId, dept.Id, "Crew A", "CA");

        var emp1 = new Employee(tenantId, "E1", "A", "B", "a@b.com", employmentType: EmploymentType.FullTime, employmentStatus: EmploymentStatus.Active, branchId: b1.Id, departmentId: dept.Id);
        var emp2 = new Employee(tenantId, "E2", "C", "D", "c@d.com", employmentType: EmploymentType.Contract, employmentStatus: EmploymentStatus.OnLeave, branchId: b1.Id, departmentId: dept.Id);

        context.Branches.AddRange(b1, b2);
        context.Departments.Add(dept);
        context.Teams.Add(team);
        context.Employees.AddRange(emp1, emp2);
        await context.SaveChangesAsync();

        var userContext = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantId };
        var dashboardService = new OrganizationDashboardService(context, userContext);

        var summary = await dashboardService.GetDashboardSummaryAsync();

        Assert.Equal(2, summary.TotalBranches);
        Assert.Equal(1, summary.ActiveBranches);
        Assert.Equal(1, summary.TotalDepartments);
        Assert.Equal(1, summary.TotalTeams);
        Assert.Equal(2, summary.TotalEmployees);
        Assert.Equal(1, summary.ActiveEmployees);
        Assert.Equal(1, summary.EmployeesByEmploymentType["FullTime"]);
        Assert.Equal(1, summary.EmployeesByEmploymentType["Contract"]);
    }

    [Fact]
    public async Task OrganizationSettingsService_InitializesDefaultsAndUpdates()
    {
        using var context = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { UserId = Guid.NewGuid(), TenantId = tenantId };
        var auditService = new TestAuditLogService();
        var settingsService = new OrganizationSettingsService(context, userContext, auditService);

        // Act - Auto initialize
        var initial = await settingsService.GetSettingsAsync();
        Assert.False(initial.EnforceBranchAssignment);
        Assert.False(initial.EnforceDepartmentAssignment);
        Assert.Equal(1, initial.FiscalYearStartMonth);

        // Act - Update
        var updated = await settingsService.UpdateSettingsAsync(new UpdateOrganizationSettingsRequest(
            EnforceBranchAssignment: true,
            EnforceDepartmentAssignment: true,
            AutoCreateEmployeeForUser: true,
            FiscalYearStartMonth: 4,
            DefaultWorkingDaysJson: "[\"Mon\",\"Tue\",\"Wed\",\"Thu\",\"Fri\"]"));

        Assert.True(updated.EnforceBranchAssignment);
        Assert.True(updated.EnforceDepartmentAssignment);
        Assert.True(updated.AutoCreateEmployeeForUser);
        Assert.Equal(4, updated.FiscalYearStartMonth);
        Assert.Single(auditService.WrittenLogs);
    }
}
