using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Maintenance;

public sealed class MaintenanceProvider : BaseEntity
{
    private MaintenanceProvider()
    {
    }

    public MaintenanceProvider(
        Guid tenantId,
        string code,
        string name,
        MaintenanceProviderType providerType = MaintenanceProviderType.InternalWorkshop,
        string? contactPerson = null,
        string? phone = null,
        string? email = null,
        string? address = null,
        Guid? branchId = null,
        string? notes = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        ProviderType = providerType;
        ContactPerson = contactPerson?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Address = address?.Trim();
        BranchId = branchId;
        Notes = notes?.Trim();
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public MaintenanceProviderType ProviderType { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string code,
        string name,
        MaintenanceProviderType providerType,
        string? contactPerson,
        string? phone,
        string? email,
        string? address,
        Guid? branchId,
        string? notes,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        ProviderType = providerType;
        ContactPerson = contactPerson?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Address = address?.Trim();
        BranchId = branchId;
        Notes = notes?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
