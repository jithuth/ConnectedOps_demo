namespace ConnectedOps.Domain.Auditing;

public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Activated = 4,
    Deactivated = 5,
    RolesUpdated = 6,
    PermissionsUpdated = 7,
    SettingsUpdated = 8,
    InvitationCreated = 9,
    InvitationAccepted = 10,
    Linked = 11,
    Unlinked = 12,
    Assigned = 13,
    Issued = 14,
    Paid = 15,
    Voided = 16,
    Refunded = 17
}