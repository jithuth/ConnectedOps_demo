using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Platform;
using ConnectedOps.Application.Security;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Security;
using ConnectedOps.Infrastructure.Auditing;
using ConnectedOps.Infrastructure.Platform;
using ConnectedOps.Infrastructure.Security;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task SecurityLogService_ThrowsException_WhenTenantContextNotResolved()
    {
        using var context = TestDbContextFactory.Create();
        var tenantContext = new TestTenantContext { TenantId = null };
        var httpAccessor = TestHttpContextHelper.CreateHttpContextAccessor();
        var service = new SecurityLogService(context, httpAccessor, tenantContext);

        var query = new SecurityLogQuery { Page = 1, PageSize = 10 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync(query));
    }

    [Fact]
    public async Task SecurityLogService_EnforcesStrictTenantIsolation_TenantACannotSeeTenantB()
    {
        using var context = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Seed logs for Tenant A and Tenant B
        var logA = new SecurityLog(
            tenantId: tenantA,
            userId: Guid.NewGuid(),
            tenantUserId: Guid.NewGuid(),
            eventType: SecurityEventType.LoginSucceeded,
            succeeded: true,
            email: "alice@tenanta.com",
            description: "Login succeeded",
            ipAddress: "1.1.1.1",
            userAgent: "AgentA",
            requestPath: "/api/auth/login",
            traceId: null,
            metadata: null);

        var logB = new SecurityLog(
            tenantId: tenantB,
            userId: Guid.NewGuid(),
            tenantUserId: Guid.NewGuid(),
            eventType: SecurityEventType.LoginSucceeded,
            succeeded: true,
            email: "bob@tenantb.com",
            description: "Login succeeded",
            ipAddress: "2.2.2.2",
            userAgent: "AgentB",
            requestPath: "/api/auth/login",
            traceId: null,
            metadata: null);

        context.SecurityLogs.AddRange(logA, logB);
        await context.SaveChangesAsync();

        // Query as Tenant A
        var tenantContext = new TestTenantContext { TenantId = tenantA };
        var httpAccessor = TestHttpContextHelper.CreateHttpContextAccessor();
        var tenantService = new SecurityLogService(context, httpAccessor, tenantContext);

        var resultA = await tenantService.GetAsync(new SecurityLogQuery { Page = 1, PageSize = 10 });

        Assert.Equal(1, resultA.TotalCount);
        Assert.Single(resultA.Items);
        Assert.Equal("alice@tenanta.com", resultA.Items[0].Email);
        Assert.DoesNotContain(resultA.Items, item => item.Email == "bob@tenantb.com");
    }

    [Fact]
    public async Task AuditLogService_EnforcesStrictTenantIsolation_TenantACannotSeeTenantB()
    {
        using var context = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var auditA = new AuditLog(
            tenantId: tenantA,
            userId: Guid.NewGuid(),
            tenantUserId: Guid.NewGuid(),
            action: AuditAction.Created,
            entityType: "Vehicle",
            entityId: "V-100",
            description: "Created vehicle",
            oldValues: null,
            newValues: "{}",
            ipAddress: "1.1.1.1",
            userAgent: "AgentA",
            requestPath: "/api/vehicles",
            traceId: null);

        var auditB = new AuditLog(
            tenantId: tenantB,
            userId: Guid.NewGuid(),
            tenantUserId: Guid.NewGuid(),
            action: AuditAction.Created,
            entityType: "Vehicle",
            entityId: "V-200",
            description: "Created vehicle",
            oldValues: null,
            newValues: "{}",
            ipAddress: "2.2.2.2",
            userAgent: "AgentB",
            requestPath: "/api/vehicles",
            traceId: null);

        context.AuditLogs.AddRange(auditA, auditB);
        await context.SaveChangesAsync();

        var tenantContext = new TestTenantContext { TenantId = tenantA };
        var userContext = new TestUserContext();
        var httpAccessor = TestHttpContextHelper.CreateHttpContextAccessor();
        var tenantService = new AuditLogService(context, tenantContext, userContext, httpAccessor);

        var resultA = await tenantService.GetAsync(new AuditLogQuery { Page = 1, PageSize = 10 });

        Assert.Equal(1, resultA.TotalCount);
        Assert.Single(resultA.Items);
        Assert.Equal("V-100", resultA.Items[0].EntityId);
        Assert.DoesNotContain(resultA.Items, item => item.EntityId == "V-200");
    }

    [Fact]
    public async Task PlatformServices_AllowCrossTenantQuerying()
    {
        using var context = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var logA = new SecurityLog(tenantA, Guid.NewGuid(), Guid.NewGuid(), SecurityEventType.LoginSucceeded, true, "a@a.com", "Desc", "1.1.1.1", "UA", "/login", null, null);
        var logB = new SecurityLog(tenantB, Guid.NewGuid(), Guid.NewGuid(), SecurityEventType.LoginSucceeded, true, "b@b.com", "Desc", "2.2.2.2", "UA", "/login", null, null);

        context.SecurityLogs.AddRange(logA, logB);
        await context.SaveChangesAsync();

        var platformSecurityService = new PlatformSecurityService(context);

        // Platform query without tenant filter returns both
        var allLogs = await platformSecurityService.GetSecurityLogsAsync(new PlatformSecurityLogQuery());
        Assert.Equal(2, allLogs.TotalCount);

        // Platform query with tenant filter returns specific tenant
        var tenantALogs = await platformSecurityService.GetSecurityLogsAsync(new PlatformSecurityLogQuery(TenantId: tenantA));
        Assert.Equal(1, tenantALogs.TotalCount);
        Assert.Equal("a@a.com", tenantALogs.Items[0].Email);
    }
}
