using ConnectedOps.Domain.Maintenance;
using FluentValidation;

namespace ConnectedOps.Application.Maintenance.Validators;

public sealed class CreateMaintenanceServiceTypeRequestValidator : AbstractValidator<CreateMaintenanceServiceTypeRequest>
{
    public CreateMaintenanceServiceTypeRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Service type code is required.")
            .MaximumLength(50).WithMessage("Service type code must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service type name is required.")
            .MaximumLength(100).WithMessage("Service type name must not exceed 100 characters.");

        RuleFor(x => x.DefaultDurationHours)
            .GreaterThanOrEqualTo(0).When(x => x.DefaultDurationHours.HasValue)
            .WithMessage("Default duration cannot be negative.");
    }
}

public sealed class UpdateMaintenanceServiceTypeRequestValidator : AbstractValidator<UpdateMaintenanceServiceTypeRequest>
{
    public UpdateMaintenanceServiceTypeRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Service type code is required.")
            .MaximumLength(50).WithMessage("Service type code must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service type name is required.")
            .MaximumLength(100).WithMessage("Service type name must not exceed 100 characters.");

        RuleFor(x => x.DefaultDurationHours)
            .GreaterThanOrEqualTo(0).When(x => x.DefaultDurationHours.HasValue)
            .WithMessage("Default duration cannot be negative.");
    }
}

public sealed class CreateMaintenanceProviderRequestValidator : AbstractValidator<CreateMaintenanceProviderRequest>
{
    public CreateMaintenanceProviderRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Provider code is required.")
            .MaximumLength(50).WithMessage("Provider code must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Provider name is required.")
            .MaximumLength(150).WithMessage("Provider name must not exceed 150 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email format.");
    }
}

public sealed class UpdateMaintenanceProviderRequestValidator : AbstractValidator<UpdateMaintenanceProviderRequest>
{
    public UpdateMaintenanceProviderRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Provider code is required.")
            .MaximumLength(50).WithMessage("Provider code must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Provider name is required.")
            .MaximumLength(150).WithMessage("Provider name must not exceed 150 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email format.");
    }
}

public sealed class CreateMaintenancePlanRequestValidator : AbstractValidator<CreateMaintenancePlanRequest>
{
    public CreateMaintenancePlanRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Plan code is required.")
            .MaximumLength(50).WithMessage("Plan code must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Plan name is required.")
            .MaximumLength(150).WithMessage("Plan name must not exceed 150 characters.");
    }
}

public sealed class UpdateMaintenancePlanRequestValidator : AbstractValidator<UpdateMaintenancePlanRequest>
{
    public UpdateMaintenancePlanRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Plan code is required.")
            .MaximumLength(50).WithMessage("Plan code must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Plan name is required.")
            .MaximumLength(150).WithMessage("Plan name must not exceed 150 characters.");
    }
}

public sealed class CreateMaintenancePlanRuleRequestValidator : AbstractValidator<CreateMaintenancePlanRuleRequest>
{
    public CreateMaintenancePlanRuleRequestValidator()
    {
        RuleFor(x => x.MaintenanceServiceTypeId)
            .NotEmpty().WithMessage("Maintenance service type is required.");

        RuleFor(x => x)
            .Must(HaveValidIntervalForScheduleType)
            .WithMessage("At least one valid interval must be specified for the selected schedule type.");

        RuleFor(x => x.ReminderBeforeKilometers)
            .GreaterThanOrEqualTo(0).When(x => x.ReminderBeforeKilometers.HasValue);
        RuleFor(x => x.ReminderBeforeEngineHours)
            .GreaterThanOrEqualTo(0).When(x => x.ReminderBeforeEngineHours.HasValue);
        RuleFor(x => x.ReminderBeforeDays)
            .GreaterThanOrEqualTo(0).When(x => x.ReminderBeforeDays.HasValue);

        RuleFor(x => x.ToleranceKilometers)
            .GreaterThanOrEqualTo(0).When(x => x.ToleranceKilometers.HasValue);
        RuleFor(x => x.ToleranceHours)
            .GreaterThanOrEqualTo(0).When(x => x.ToleranceHours.HasValue);
        RuleFor(x => x.ToleranceDays)
            .GreaterThanOrEqualTo(0).When(x => x.ToleranceDays.HasValue);
    }

    private static bool HaveValidIntervalForScheduleType(CreateMaintenancePlanRuleRequest rule)
    {
        return rule.ScheduleType switch
        {
            MaintenanceScheduleType.Distance =>
                (rule.IntervalKilometers.HasValue && rule.IntervalKilometers.Value > 0) ||
                (rule.IntervalMiles.HasValue && rule.IntervalMiles.Value > 0),

            MaintenanceScheduleType.EngineHours =>
                rule.IntervalEngineHours.HasValue && rule.IntervalEngineHours.Value > 0,

            MaintenanceScheduleType.Calendar =>
                (rule.IntervalDays.HasValue && rule.IntervalDays.Value > 0) ||
                (rule.IntervalMonths.HasValue && rule.IntervalMonths.Value > 0),

            MaintenanceScheduleType.DistanceOrCalendar or MaintenanceScheduleType.DistanceAndCalendar =>
                ((rule.IntervalKilometers.HasValue && rule.IntervalKilometers.Value > 0) || (rule.IntervalMiles.HasValue && rule.IntervalMiles.Value > 0)) &&
                ((rule.IntervalDays.HasValue && rule.IntervalDays.Value > 0) || (rule.IntervalMonths.HasValue && rule.IntervalMonths.Value > 0)),

            MaintenanceScheduleType.EngineHoursOrCalendar =>
                (rule.IntervalEngineHours.HasValue && rule.IntervalEngineHours.Value > 0) &&
                ((rule.IntervalDays.HasValue && rule.IntervalDays.Value > 0) || (rule.IntervalMonths.HasValue && rule.IntervalMonths.Value > 0)),

            _ => true
        };
    }
}

public sealed class UpdateMaintenancePlanRuleRequestValidator : AbstractValidator<UpdateMaintenancePlanRuleRequest>
{
    public UpdateMaintenancePlanRuleRequestValidator()
    {
        RuleFor(x => x.MaintenanceServiceTypeId)
            .NotEmpty().WithMessage("Maintenance service type is required.");

        RuleFor(x => x)
            .Must(HaveValidIntervalForScheduleType)
            .WithMessage("At least one valid interval must be specified for the selected schedule type.");
    }

    private static bool HaveValidIntervalForScheduleType(UpdateMaintenancePlanRuleRequest rule)
    {
        return rule.ScheduleType switch
        {
            MaintenanceScheduleType.Distance =>
                (rule.IntervalKilometers.HasValue && rule.IntervalKilometers.Value > 0) ||
                (rule.IntervalMiles.HasValue && rule.IntervalMiles.Value > 0),

            MaintenanceScheduleType.EngineHours =>
                rule.IntervalEngineHours.HasValue && rule.IntervalEngineHours.Value > 0,

            MaintenanceScheduleType.Calendar =>
                (rule.IntervalDays.HasValue && rule.IntervalDays.Value > 0) ||
                (rule.IntervalMonths.HasValue && rule.IntervalMonths.Value > 0),

            MaintenanceScheduleType.DistanceOrCalendar or MaintenanceScheduleType.DistanceAndCalendar =>
                ((rule.IntervalKilometers.HasValue && rule.IntervalKilometers.Value > 0) || (rule.IntervalMiles.HasValue && rule.IntervalMiles.Value > 0)) &&
                ((rule.IntervalDays.HasValue && rule.IntervalDays.Value > 0) || (rule.IntervalMonths.HasValue && rule.IntervalMonths.Value > 0)),

            MaintenanceScheduleType.EngineHoursOrCalendar =>
                (rule.IntervalEngineHours.HasValue && rule.IntervalEngineHours.Value > 0) &&
                ((rule.IntervalDays.HasValue && rule.IntervalDays.Value > 0) || (rule.IntervalMonths.HasValue && rule.IntervalMonths.Value > 0)),

            _ => true
        };
    }
}

public sealed class AssignVehiclePlanRequestValidator : AbstractValidator<AssignVehiclePlanRequest>
{
    public AssignVehiclePlanRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle is required.");

        RuleFor(x => x.MaintenancePlanId)
            .NotEmpty().WithMessage("Maintenance plan is required.");

        RuleFor(x => x.EffectiveFromUtc)
            .NotEmpty().WithMessage("Effective from date is required.");

        RuleFor(x => x.BaselineOdometer)
            .GreaterThanOrEqualTo(0).When(x => x.BaselineOdometer.HasValue)
            .WithMessage("Baseline odometer cannot be negative.");

        RuleFor(x => x.BaselineEngineHours)
            .GreaterThanOrEqualTo(0).When(x => x.BaselineEngineHours.HasValue)
            .WithMessage("Baseline engine hours cannot be negative.");
    }
}

public sealed class CreateMaintenanceRecordRequestValidator : AbstractValidator<CreateMaintenanceRecordRequest>
{
    public CreateMaintenanceRecordRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle is required.");

        RuleFor(x => x.MaintenanceServiceTypeId)
            .NotEmpty().WithMessage("Maintenance service type is required.");

        RuleFor(x => x.ServiceDateUtc)
            .NotEmpty().WithMessage("Service date is required.");

        RuleFor(x => x.OdometerReading)
            .GreaterThanOrEqualTo(0).When(x => x.OdometerReading.HasValue)
            .WithMessage("Odometer reading cannot be negative.");

        RuleFor(x => x.EngineHours)
            .GreaterThanOrEqualTo(0).When(x => x.EngineHours.HasValue)
            .WithMessage("Engine hours cannot be negative.");
    }
}

public sealed class CompleteMaintenanceRecordRequestValidator : AbstractValidator<CompleteMaintenanceRecordRequest>
{
    public CompleteMaintenanceRecordRequestValidator()
    {
        RuleFor(x => x.CompletedDateTimeUtc)
            .NotEmpty().WithMessage("Completed date is required.");

        RuleFor(x => x.FinalOdometer)
            .GreaterThanOrEqualTo(0).When(x => x.FinalOdometer.HasValue)
            .WithMessage("Final odometer cannot be negative.");

        RuleFor(x => x.FinalEngineHours)
            .GreaterThanOrEqualTo(0).When(x => x.FinalEngineHours.HasValue)
            .WithMessage("Final engine hours cannot be negative.");
    }
}

public sealed class AddMaintenancePartRequestValidator : AbstractValidator<AddMaintenancePartRequest>
{
    public AddMaintenancePartRequestValidator()
    {
        RuleFor(x => x.PartName)
            .NotEmpty().WithMessage("Part name is required.")
            .MaximumLength(150).WithMessage("Part name must not exceed 150 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue)
            .WithMessage("Unit cost cannot be negative.");
    }
}

public sealed class AddMaintenanceLabourRequestValidator : AbstractValidator<AddMaintenanceLabourRequest>
{
    public AddMaintenanceLabourRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(200).WithMessage("Description must not exceed 200 characters.");

        RuleFor(x => x.Hours)
            .GreaterThan(0).WithMessage("Hours must be greater than zero.");

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0).When(x => x.HourlyRate.HasValue)
            .WithMessage("Hourly rate cannot be negative.");
    }
}

public sealed class AddMaintenanceExpenseRequestValidator : AbstractValidator<AddMaintenanceExpenseRequest>
{
    public AddMaintenanceExpenseRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(200).WithMessage("Description must not exceed 200 characters.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount cannot be negative.");
    }
}

public sealed class AddMaintenanceDocumentRequestValidator : AbstractValidator<AddMaintenanceDocumentRequest>
{
    public AddMaintenanceDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required.")
            .MaximumLength(150).WithMessage("Title must not exceed 150 characters.");

        RuleFor(x => x.FileObjectKey)
            .NotEmpty().WithMessage("File object key is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.");
    }
}
