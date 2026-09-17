using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Dispatch;

namespace ConnectedOps.Domain.Tracking;

public sealed class PublicTrackingToken : BaseEntity
{
    private PublicTrackingToken()
    {
    }

    public PublicTrackingToken(
        Guid tenantId,
        Guid dispatchJobId,
        string customerName,
        string customerPhone,
        DateTime expiresAtUtc,
        string? token = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (dispatchJobId == Guid.Empty)
            throw new ArgumentException("DispatchJobId is required.", nameof(dispatchJobId));

        TenantId = tenantId;
        DispatchJobId = dispatchJobId;
        CustomerName = customerName.Trim();
        CustomerPhone = customerPhone.Trim();
        ExpiresAtUtc = expiresAtUtc;
        Token = string.IsNullOrWhiteSpace(token) ? Guid.NewGuid().ToString("N") : token.Trim();
    }

    public Guid TenantId { get; private set; }

    public Guid DispatchJobId { get; private set; }
    public DispatchJob DispatchJob { get; set; } = null!;

    public string Token { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }
    public int AccessCount { get; private set; }
    public DateTime? LastAccessedAtUtc { get; private set; }

    public int? Rating { get; private set; }
    public string? FeedbackComment { get; private set; }
    public DateTime? RatedAtUtc { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;

    public void RecordAccess()
    {
        AccessCount++;
        LastAccessedAtUtc = DateTime.UtcNow;
    }

    public void SubmitFeedback(int rating, string? comment)
    {
        Rating = Math.Clamp(rating, 1, 5);
        FeedbackComment = comment?.Trim();
        RatedAtUtc = DateTime.UtcNow;
    }
}
