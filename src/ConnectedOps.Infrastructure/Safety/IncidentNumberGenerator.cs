using ConnectedOps.Application.Safety;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Safety;

public sealed class IncidentNumberGenerator : IIncidentNumberGenerator
{
    private readonly ConnectedOpsDbContext _dbContext;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public IncidentNumberGenerator(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateIncidentNumberAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            int year = DateTime.UtcNow.Year;
            string prefix = $"INC-{year}-";

            var maxNumber = await _dbContext.SafetyIncidents
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IncidentNumber.StartsWith(prefix))
                .Select(x => x.IncidentNumber)
                .OrderByDescending(x => x)
                .FirstOrDefaultAsync(cancellationToken);

            int nextSequence = 1;
            if (!string.IsNullOrWhiteSpace(maxNumber) && maxNumber.Length >= prefix.Length)
            {
                var seqPart = maxNumber[prefix.Length..];
                if (int.TryParse(seqPart, out int parsed))
                {
                    nextSequence = parsed + 1;
                }
            }

            return $"{prefix}{nextSequence:D6}";
        }
        finally
        {
            _lock.Release();
        }
    }
}
