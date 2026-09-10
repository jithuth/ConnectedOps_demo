using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Platform;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class PlatformTenantServiceTests
{
    [Fact]
    public async Task GetTenantsAsync_ReturnsPagedAndFilteredTenants()
    {
        using var context = TestDbContextFactory.Create();
        var service = new PlatformTenantService(context);

        var tenant1 = new Tenant("Acme Logistics", "ACME", "info@acme.com");
        tenant1.UpdateContact("info@acme.com", "123", "US");

        var tenant2 = new Tenant("Beta Trans", "BETA", "info@beta.com");
        tenant2.UpdateContact("info@beta.com", "456", "CA");
        tenant2.Suspend();

        var tenant3 = new Tenant("Gamma Freight", "GAMMA", "info@gamma.com");
        tenant3.UpdateContact("info@gamma.com", "789", "US");

        context.Tenants.AddRange(tenant1, tenant2, tenant3);
        await context.SaveChangesAsync();

        // 1. Query all
        var resultAll = await service.GetTenantsAsync(new PlatformTenantQuery(Page: 1, PageSize: 10));
        Assert.Equal(3, resultAll.TotalCount);
        Assert.Equal(3, resultAll.Items.Count);

        // 2. Query search
        var resultSearch = await service.GetTenantsAsync(new PlatformTenantQuery(Search: "Acme"));
        Assert.Equal(1, resultSearch.TotalCount);
        Assert.Equal("ACME", resultSearch.Items.First().Code);

        // 3. Query status
        var resultStatus = await service.GetTenantsAsync(new PlatformTenantQuery(Status: TenantStatus.Suspended));
        Assert.Equal(1, resultStatus.TotalCount);
        Assert.Equal("BETA", resultStatus.Items.First().Code);

        // 4. Query country
        var resultCountry = await service.GetTenantsAsync(new PlatformTenantQuery(Country: "US"));
        Assert.Equal(2, resultCountry.TotalCount);
    }

    [Fact]
    public async Task GetTenantsAsync_ClampsPageSizeToMaximum100()
    {
        using var context = TestDbContextFactory.Create();
        var service = new PlatformTenantService(context);

        var result = await service.GetTenantsAsync(new PlatformTenantQuery(Page: 1, PageSize: 500));
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task CreateTenantAsync_CreatesTenant_AndPreventsDuplicates()
    {
        using var context = TestDbContextFactory.Create();
        var service = new PlatformTenantService(context);

        var request = new CreatePlatformTenantRequest("Delta Fleet", "DELTA", "contact@delta.com", "555-1234", "US");
        var created = await service.CreateTenantAsync(request);

        Assert.Equal("Delta Fleet", created.Name);
        Assert.Equal("DELTA", created.Code);
        Assert.Equal(TenantStatus.Active, created.Status);

        // Duplicate code must throw InvalidOperationException
        var duplicateRequest = new CreatePlatformTenantRequest("Delta Fleet 2", "DELTA");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTenantAsync(duplicateRequest));
    }

    [Fact]
    public async Task TenantStatusTransitions_Activate_Suspend_Disable_WorkCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var service = new PlatformTenantService(context);

        var tenant = new Tenant("Epsilon Transport", "EPSILON");
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Suspend
        await service.SuspendTenantAsync(tenant.Id);
        var suspended = await context.Tenants.FindAsync(tenant.Id);
        Assert.Equal(TenantStatus.Suspended, suspended!.Status);

        // Activate
        await service.ActivateTenantAsync(tenant.Id);
        var activated = await context.Tenants.FindAsync(tenant.Id);
        Assert.Equal(TenantStatus.Active, activated!.Status);

        // Disable
        await service.DisableTenantAsync(tenant.Id);
        var disabled = await context.Tenants.FindAsync(tenant.Id);
        Assert.Equal(TenantStatus.Disabled, disabled!.Status);
    }

    [Fact]
    public async Task TenantOperations_ThrowKeyNotFound_WhenTenantDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var service = new PlatformTenantService(context);
        var nonExistentId = Guid.NewGuid();

        var result = await service.GetTenantByIdAsync(nonExistentId);
        Assert.Null(result);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ActivateTenantAsync(nonExistentId));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SuspendTenantAsync(nonExistentId));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DisableTenantAsync(nonExistentId));
    }
}
