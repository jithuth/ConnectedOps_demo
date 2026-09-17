using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Hos;

public sealed class HosRuleConfiguration : BaseEntity
{
    private HosRuleConfiguration()
    {
    }

    public HosRuleConfiguration(
        Guid tenantId,
        HosPresetType presetType = HosPresetType.GccUaeStandard,
        double maxDrivingHoursPerShift = 10.0,
        double maxShiftDutyHours = 12.0,
        double driveHoursBeforeMandatoryBreak = 4.5,
        int mandatoryBreakMinutes = 45,
        double minConsecutiveOffDutyHours = 8.0,
        int cycleDays = 7,
        double cycleMaxDutyHours = 60.0)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        PresetType = presetType;
        MaxDrivingHoursPerShift = maxDrivingHoursPerShift > 0 ? maxDrivingHoursPerShift : 10.0;
        MaxShiftDutyHours = maxShiftDutyHours > 0 ? maxShiftDutyHours : 12.0;
        DriveHoursBeforeMandatoryBreak = driveHoursBeforeMandatoryBreak > 0 ? driveHoursBeforeMandatoryBreak : 4.5;
        MandatoryBreakMinutes = mandatoryBreakMinutes >= 0 ? mandatoryBreakMinutes : 30;
        MinConsecutiveOffDutyHours = minConsecutiveOffDutyHours > 0 ? minConsecutiveOffDutyHours : 8.0;
        CycleDays = cycleDays > 0 ? cycleDays : 7;
        CycleMaxDutyHours = cycleMaxDutyHours > 0 ? cycleMaxDutyHours : 60.0;
    }

    public Guid TenantId { get; private set; }
    public HosPresetType PresetType { get; private set; }
    public double MaxDrivingHoursPerShift { get; private set; }
    public double MaxShiftDutyHours { get; private set; }
    public double DriveHoursBeforeMandatoryBreak { get; private set; }
    public int MandatoryBreakMinutes { get; private set; }
    public double MinConsecutiveOffDutyHours { get; private set; }
    public int CycleDays { get; private set; }
    public double CycleMaxDutyHours { get; private set; }

    public void ApplyPreset(HosPresetType preset, Guid? updatedBy = null)
    {
        PresetType = preset;
        switch (preset)
        {
            case HosPresetType.GccUaeStandard:
                MaxDrivingHoursPerShift = 10.0;
                MaxShiftDutyHours = 12.0;
                DriveHoursBeforeMandatoryBreak = 4.5;
                MandatoryBreakMinutes = 45;
                MinConsecutiveOffDutyHours = 8.0;
                CycleDays = 7;
                CycleMaxDutyHours = 60.0;
                break;

            case HosPresetType.EuTachograph:
                MaxDrivingHoursPerShift = 9.0;
                MaxShiftDutyHours = 13.0;
                DriveHoursBeforeMandatoryBreak = 4.5;
                MandatoryBreakMinutes = 45;
                MinConsecutiveOffDutyHours = 11.0;
                CycleDays = 7;
                CycleMaxDutyHours = 56.0;
                break;

            case HosPresetType.UsFmcsa:
                MaxDrivingHoursPerShift = 11.0;
                MaxShiftDutyHours = 14.0;
                DriveHoursBeforeMandatoryBreak = 8.0;
                MandatoryBreakMinutes = 30;
                MinConsecutiveOffDutyHours = 10.0;
                CycleDays = 8;
                CycleMaxDutyHours = 70.0;
                break;

            case HosPresetType.Custom:
                // Retains existing or modified custom parameters
                break;
        }

        MarkUpdated(updatedBy);
    }

    public void UpdateCustomPolicy(
        double maxDrivingHours,
        double maxShiftHours,
        double driveHoursBeforeBreak,
        int breakMinutes,
        double minOffDutyHours,
        int cycleDays,
        double cycleMaxHours,
        Guid? updatedBy = null)
    {
        PresetType = HosPresetType.Custom;
        MaxDrivingHoursPerShift = maxDrivingHours > 0 ? maxDrivingHours : 10.0;
        MaxShiftDutyHours = maxShiftHours > 0 ? maxShiftHours : 12.0;
        DriveHoursBeforeMandatoryBreak = driveHoursBeforeBreak > 0 ? driveHoursBeforeBreak : 4.5;
        MandatoryBreakMinutes = breakMinutes >= 0 ? breakMinutes : 30;
        MinConsecutiveOffDutyHours = minOffDutyHours > 0 ? minOffDutyHours : 8.0;
        CycleDays = cycleDays > 0 ? cycleDays : 7;
        CycleMaxDutyHours = cycleMaxHours > 0 ? cycleMaxHours : 60.0;
        MarkUpdated(updatedBy);
    }
}
