using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Infrastructure.Compliance;

public sealed class ComplianceStatusService : IComplianceStatusService
{
    private readonly IComplianceExpiryService _expiryService;

    public ComplianceStatusService(IComplianceExpiryService expiryService)
    {
        _expiryService = expiryService;
    }

    public ComplianceStatus DetermineStatus(
        DateTime? expiryDateUtc,
        int reminderDays,
        bool isVerified,
        bool hasActiveException,
        bool recordExists,
        DateTime? nowUtc = null)
    {
        if (hasActiveException)
            return ComplianceStatus.Valid;

        if (!recordExists)
            return ComplianceStatus.Missing;

        if (_expiryService.IsExpired(expiryDateUtc, nowUtc))
            return ComplianceStatus.Expired;

        if (!isVerified)
            return ComplianceStatus.PendingVerification;

        if (_expiryService.IsExpiringSoon(expiryDateUtc, reminderDays, nowUtc))
            return ComplianceStatus.ExpiringSoon;

        return ComplianceStatus.Valid;
    }

    public ComplianceStatus DetermineOverallStatus(IEnumerable<ComplianceStatus> requirementStatuses)
    {
        var statusList = requirementStatuses.ToList();
        if (statusList.Count == 0)
            return ComplianceStatus.Valid;

        if (statusList.Contains(ComplianceStatus.Expired))
            return ComplianceStatus.Expired;

        if (statusList.Contains(ComplianceStatus.Missing))
            return ComplianceStatus.Missing;

        if (statusList.Contains(ComplianceStatus.Rejected))
            return ComplianceStatus.Rejected;

        if (statusList.Contains(ComplianceStatus.Suspended))
            return ComplianceStatus.Suspended;

        if (statusList.Contains(ComplianceStatus.ExpiringSoon))
            return ComplianceStatus.ExpiringSoon;

        if (statusList.Contains(ComplianceStatus.PendingVerification))
            return ComplianceStatus.PendingVerification;

        return ComplianceStatus.Valid;
    }
}
