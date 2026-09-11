using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Auth;
using TenantSettingsEntity = ConnectedOps.Domain.Tenancy.TenantSettings;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Accounting;
using ConnectedOps.Domain.Billing;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Domain.Security;

namespace ConnectedOps.Infrastructure.Persistence;


public sealed class ConnectedOpsDbContext
    : IdentityDbContext<
        ApplicationUser,
        IdentityRole<Guid>,
        Guid>
{
    public ConnectedOpsDbContext(
        DbContextOptions<ConnectedOpsDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformSettings> PlatformSettings =>
        Set<PlatformSettings>();

    public DbSet<TenantInvitation> TenantInvitations
    => Set<TenantInvitation>();

    public DbSet<AuditLog> AuditLogs =>
    Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens
    => Set<RefreshToken>();

    public DbSet<SecurityLog> SecurityLogs =>
    Set<SecurityLog>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();

    public DbSet<Permission> Permissions =>
    Set<Permission>();

    public DbSet<TenantRole> TenantRoles =>
        Set<TenantRole>();

    public DbSet<TenantSettingsEntity> TenantSettings =>
    Set<TenantSettingsEntity>();

    public DbSet<TenantUserRole> TenantUserRoles =>
        Set<TenantUserRole>();

    public DbSet<RolePermission> RolePermissions =>
        Set<RolePermission>();

    public DbSet<OrganizationProfile> OrganizationProfiles =>
        Set<OrganizationProfile>();

    public DbSet<Branch> Branches =>
        Set<Branch>();

    public DbSet<Location> Locations =>
        Set<Location>();

    public DbSet<Department> Departments =>
        Set<Department>();

    public DbSet<Team> Teams =>
        Set<Team>();

    public DbSet<Employee> Employees =>
        Set<Employee>();

    public DbSet<OrganizationSettings> OrganizationSettings =>
        Set<OrganizationSettings>();

    public DbSet<SubscriptionPlan> SubscriptionPlans =>
        Set<SubscriptionPlan>();

    public DbSet<TenantSubscription> TenantSubscriptions =>
        Set<TenantSubscription>();

    public DbSet<Invoice> Invoices =>
        Set<Invoice>();

    public DbSet<InvoiceItem> InvoiceItems =>
        Set<InvoiceItem>();

    public DbSet<PaymentTransaction> PaymentTransactions =>
        Set<PaymentTransaction>();

    public DbSet<GeneralLedgerAccount> GeneralLedgerAccounts =>
        Set<GeneralLedgerAccount>();

    public DbSet<LedgerEntry> LedgerEntries =>
        Set<LedgerEntry>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ConnectedOpsDbContext).Assembly);
    }
}