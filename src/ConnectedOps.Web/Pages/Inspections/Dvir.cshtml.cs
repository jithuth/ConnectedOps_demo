using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Inspections;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Inspections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Inspections;

[Authorize]
public class DvirModel : PageModel
{
    private readonly IDvirService _dvirService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;

    public DvirModel(
        IDvirService dvirService,
        IVehicleService vehicleService,
        IDriverService driverService)
    {
        _dvirService = dvirService;
        _vehicleService = vehicleService;
        _driverService = driverService;
    }

    public DvirDashboardDto Metrics { get; private set; } = null!;
    public PagedResult<DvirInspectionDto> Inspections { get; private set; } = null!;
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public DvirStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Metrics = await _dvirService.GetDashboardAsync(cancellationToken);
        Inspections = await _dvirService.GetInspectionsPagedAsync(
            new DvirFilterRequest(Status: StatusFilter, PageNumber: PageNumber, PageSize: 20),
            cancellationToken);

        var vehiclesPaged = await _vehicleService.GetVehiclesPagedAsync(
            new VehicleQueryParameters { PageNumber = 1, PageSize = 100 },
            cancellationToken);
        Vehicles = vehiclesPaged.Items;

        var driversPaged = await _driverService.GetDriversPagedAsync(
            new DriverQueryParameters { PageNumber = 1, PageSize = 100 },
            cancellationToken);
        Drivers = driversPaged.Items;
    }

    public async Task<IActionResult> OnPostCreateInspectionAsync(
        Guid vehicleId,
        Guid driverId,
        DvirType inspectionType,
        decimal odometer,
        string locationName,
        string? remarks,
        string? driverSignature,
        bool brakesPassed,
        bool tiresPassed,
        bool lightsPassed,
        bool steeringPassed,
        bool hornPassed,
        bool mirrorsPassed,
        bool defectIsCritical,
        string? defectDescription,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = new List<CreateDvirItemCheckRequest>
            {
                new("Brakes", "Service Brakes & Parking Brake", brakesPassed, brakesPassed ? null : (defectIsCritical ? DefectSeverity.Critical : DefectSeverity.Major), brakesPassed ? null : defectDescription),
                new("Tires", "Tire Pressure & Tread Depth", tiresPassed, tiresPassed ? null : (defectIsCritical ? DefectSeverity.Critical : DefectSeverity.Major), tiresPassed ? null : defectDescription),
                new("Lighting", "Headlights, Turn Signals & Brake Lights", lightsPassed, lightsPassed ? null : DefectSeverity.Minor, lightsPassed ? null : defectDescription),
                new("Steering", "Steering Mechanism & Play", steeringPassed, steeringPassed ? null : (defectIsCritical ? DefectSeverity.Critical : DefectSeverity.Major), steeringPassed ? null : defectDescription),
                new("Safety", "Horn & Windshield Wipers", hornPassed, hornPassed ? null : DefectSeverity.Minor, hornPassed ? null : defectDescription),
                new("Mirrors", "Rear View & Side Mirrors", mirrorsPassed, mirrorsPassed ? null : DefectSeverity.Minor, mirrorsPassed ? null : defectDescription)
            };

            var request = new CreateDvirRequest(
                VehicleId: vehicleId,
                DriverId: driverId,
                InspectionType: inspectionType,
                Odometer: odometer,
                LocationName: locationName,
                Remarks: remarks,
                DriverSignatureData: string.IsNullOrWhiteSpace(driverSignature) ? "DIGITAL_SIG_VALID" : driverSignature,
                Items: items);

            var result = await _dvirService.CreateInspectionAsync(request, cancellationToken);

            if (result.Status == DvirStatus.VehicleGrounded)
            {
                SuccessMessage = $"DVIR {result.InspectionNumber} logged. CRITICAL DEFECT DETECTED: Vehicle has been grounded automatically.";
            }
            else
            {
                SuccessMessage = $"DVIR {result.InspectionNumber} recorded successfully.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCertifyAsync(
        Guid inspectionId,
        string mechanicName,
        string? mechanicNotes,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dvirService.CertifyByMechanicAsync(
                inspectionId,
                new SignOffDvirRequest(mechanicName, mechanicNotes, "MECH_SIG_APPROVED"),
                cancellationToken);

            SuccessMessage = $"Inspection certified safe by mechanic {mechanicName}. Grounding condition lifted.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
