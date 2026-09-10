namespace ConnectedOps.Application.Tenants;

public sealed record CreateTenantResult(
    Guid TenantId,
    Guid UserId,
    Guid TenantUserId,
    string TenantCode,
    string OwnerEmail);