using System.Diagnostics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Web.Pages.Platform;

public sealed class SystemHealthModel : PageModel
{
    private readonly ConnectedOpsDbContext _dbContext;

    public SystemHealthModel(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool DbHealthy { get; private set; }
    public long DbLatencyMs { get; private set; }
    public string DbProvider { get; private set; } = "Microsoft.EntityFrameworkCore.SqlServer";
    public long MemoryAllocatedMb { get; private set; }
    public string DotNetVersion { get; private set; } = string.Empty;
    public string OsDescription { get; private set; } = string.Empty;
    public DateTime ServerTimeUtc { get; private set; }
    public TimeSpan ProcessUptime { get; private set; }

    public async Task OnGetAsync()
    {
        DotNetVersion = Environment.Version.ToString();
        OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        ServerTimeUtc = DateTime.UtcNow;
        ProcessUptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        MemoryAllocatedMb = GC.GetTotalMemory(false) / (1024 * 1024);

        var sw = Stopwatch.StartNew();
        try
        {
            DbHealthy = await _dbContext.Database.CanConnectAsync(HttpContext.RequestAborted);
            sw.Stop();
            DbLatencyMs = sw.ElapsedMilliseconds;
        }
        catch
        {
            sw.Stop();
            DbHealthy = false;
            DbLatencyMs = sw.ElapsedMilliseconds;
        }
    }
}
