using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.FleetOperations;

public sealed class FleetShift : BaseEntity
{
    private FleetShift()
    {
    }

    public FleetShift(
        Guid tenantId,
        string name,
        string code,
        TimeOnly startTime,
        TimeOnly endTime,
        DayOfWeekFlags daysOfWeek = DayOfWeekFlags.All,
        Guid? branchId = null,
        string? description = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        SetName(name);
        SetCode(code);
        StartTime = startTime;
        EndTime = endTime;
        CrossesMidnight = endTime <= startTime;
        DaysOfWeek = daysOfWeek;
        BranchId = branchId;
        Description = description?.Trim();
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool CrossesMidnight { get; private set; }
    public DayOfWeekFlags DaysOfWeek { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public void UpdateDetails(
        string name,
        string code,
        TimeOnly startTime,
        TimeOnly endTime,
        DayOfWeekFlags daysOfWeek,
        Guid? branchId,
        string? description)
    {
        SetName(name);
        SetCode(code);
        StartTime = startTime;
        EndTime = endTime;
        CrossesMidnight = endTime <= startTime;
        DaysOfWeek = daysOfWeek;
        BranchId = branchId;
        Description = description?.Trim();
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public bool IsScheduledOn(DayOfWeek day)
    {
        var flag = day switch
        {
            DayOfWeek.Sunday => DayOfWeekFlags.Sunday,
            DayOfWeek.Monday => DayOfWeekFlags.Monday,
            DayOfWeek.Tuesday => DayOfWeekFlags.Tuesday,
            DayOfWeek.Wednesday => DayOfWeekFlags.Wednesday,
            DayOfWeek.Thursday => DayOfWeekFlags.Thursday,
            DayOfWeek.Friday => DayOfWeekFlags.Friday,
            DayOfWeek.Saturday => DayOfWeekFlags.Saturday,
            _ => DayOfWeekFlags.None
        };

        return (DaysOfWeek & flag) != 0;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Shift name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Shift code cannot be empty.", nameof(code));
        Code = code.Trim().ToUpperInvariant();
    }
}
