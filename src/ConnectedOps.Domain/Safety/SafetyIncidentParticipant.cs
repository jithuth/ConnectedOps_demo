using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Safety;

public sealed class SafetyIncidentParticipant : BaseEntity
{
    private SafetyIncidentParticipant()
    {
    }

    public SafetyIncidentParticipant(
        Guid tenantId,
        Guid safetyIncidentId,
        SafetyParticipantType participantType,
        string role,
        Guid? driverId = null,
        Guid? employeeId = null,
        string? name = null,
        bool injuryReported = false,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (safetyIncidentId == Guid.Empty)
            throw new ArgumentException("SafetyIncidentId is required.", nameof(safetyIncidentId));
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required.", nameof(role));

        TenantId = tenantId;
        SafetyIncidentId = safetyIncidentId;
        ParticipantType = participantType;
        Role = role.Trim();
        DriverId = driverId;
        EmployeeId = employeeId;
        Name = name?.Trim();
        InjuryReported = injuryReported;
        Notes = notes?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid SafetyIncidentId { get; private set; }
    public SafetyIncident SafetyIncident { get; private set; } = null!;

    public SafetyParticipantType ParticipantType { get; private set; }
    public string Role { get; private set; } = string.Empty;

    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }

    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public string? Name { get; private set; }
    public bool InjuryReported { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        SafetyParticipantType participantType,
        string role,
        Guid? driverId,
        Guid? employeeId,
        string? name,
        bool injuryReported,
        string? notes,
        Guid? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required.", nameof(role));

        ParticipantType = participantType;
        Role = role.Trim();
        DriverId = driverId;
        EmployeeId = employeeId;
        Name = name?.Trim();
        InjuryReported = injuryReported;
        Notes = notes?.Trim();
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }
}
