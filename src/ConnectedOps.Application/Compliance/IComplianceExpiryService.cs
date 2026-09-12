namespace ConnectedOps.Application.Compliance;

public interface IComplianceExpiryService
{
    int CalculateDaysRemaining(DateTime? expiryDateUtc, DateTime? nowUtc = null);
    bool IsExpired(DateTime? expiryDateUtc, DateTime? nowUtc = null);
    bool IsExpiringSoon(DateTime? expiryDateUtc, int reminderDays, DateTime? nowUtc = null);
}
