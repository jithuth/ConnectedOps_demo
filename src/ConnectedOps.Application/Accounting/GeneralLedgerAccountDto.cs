using ConnectedOps.Domain.Accounting;

namespace ConnectedOps.Application.Accounting;

public sealed record GeneralLedgerAccountDto(
    Guid Id,
    Guid TenantId,
    string AccountCode,
    string AccountName,
    AccountCategory Category,
    string? Description,
    bool IsActive,
    bool IsSystem,
    decimal CurrentBalance,
    DateTime CreatedAtUtc);

public sealed record CreateGeneralLedgerAccountRequest
{
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public AccountCategory Category { get; init; }
    public string? Description { get; init; }
}

public sealed record UpdateGeneralLedgerAccountRequest
{
    public string AccountName { get; init; } = string.Empty;
    public string? Description { get; init; }
}
