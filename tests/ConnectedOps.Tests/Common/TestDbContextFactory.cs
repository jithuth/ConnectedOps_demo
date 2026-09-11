using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Tests.Common;

public static class TestDbContextFactory
{
    public static ConnectedOpsDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ConnectedOpsDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ConnectedOpsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
