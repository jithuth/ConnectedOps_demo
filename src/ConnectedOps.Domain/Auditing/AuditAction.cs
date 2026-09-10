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
    InvitationAccepted = 10
}