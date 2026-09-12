using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Application.Compliance;

public interface IComplianceStatusService
{
    ComplianceStatus DetermineStatus(
        DateTime? expiryDateUtc,
        int reminderDays,
        bool isVerified,
        bool hasActiveException,
        bool recordExists,
        DateTime? nowUtc = null);

    ComplianceStatus DetermineOverallStatus(IEnumerable<ComplianceStatus> requirementStatuses);
}
