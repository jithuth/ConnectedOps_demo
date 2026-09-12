using ConnectedOps.Application.Compliance;

namespace ConnectedOps.Infrastructure.Compliance;

public sealed class ComplianceExpiryService : IComplianceExpiryService
{
    public int CalculateDaysRemaining(DateTime? expiryDateUtc, DateTime? nowUtc = null)
    {
        if (!expiryDateUtc.HasValue)
            return int.MaxValue;

        var now = (nowUtc ?? DateTime.UtcNow).Date;
        var expiry = expiryDateUtc.Value.Date;
        return (expiry - now).Days;
    }

    public bool IsExpired(DateTime? expiryDateUtc, DateTime? nowUtc = null)
    {
        if (!expiryDateUtc.HasValue)
            return false;

        var now = (nowUtc ?? DateTime.UtcNow).Date;
        return expiryDateUtc.Value.Date < now;
    }

    public bool IsExpiringSoon(DateTime? expiryDateUtc, int reminderDays, DateTime? nowUtc = null)
    {
        if (!expiryDateUtc.HasValue || reminderDays <= 0)
            return false;

        if (IsExpired(expiryDateUtc, nowUtc))
            return false;

        var daysRemaining = CalculateDaysRemaining(expiryDateUtc, nowUtc);
        return daysRemaining >= 0 && daysRemaining <= reminderDays;
    }
}
