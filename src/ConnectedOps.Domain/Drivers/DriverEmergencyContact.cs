using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Drivers;

public sealed class DriverEmergencyContact : BaseEntity
{
    private DriverEmergencyContact()
    {
    }

    public DriverEmergencyContact(
        Guid tenantId,
        Guid driverId,
        string name,
        string relationship,
        string phone,
        string? alternatePhone = null,
        bool isPrimary = false)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Contact name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(relationship))
            throw new ArgumentException("Relationship is required.", nameof(relationship));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number is required.", nameof(phone));

        TenantId = tenantId;
        DriverId = driverId;
        Name = name.Trim();
        Relationship = relationship.Trim();
        Phone = phone.Trim();
        AlternatePhone = alternatePhone?.Trim();
        IsPrimary = isPrimary;
    }

    public Guid TenantId { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Relationship { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? AlternatePhone { get; private set; }
    public bool IsPrimary { get; private set; }

    public void Update(string name, string relationship, string phone, string? alternatePhone, bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Contact name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(relationship))
            throw new ArgumentException("Relationship is required.", nameof(relationship));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number is required.", nameof(phone));

        Name = name.Trim();
        Relationship = relationship.Trim();
        Phone = phone.Trim();
        AlternatePhone = alternatePhone?.Trim();
        IsPrimary = isPrimary;
        MarkUpdated();
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        MarkUpdated();
    }
}
