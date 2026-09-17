using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.TollsAndFines;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.TollsAndFines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.TollsAndFines;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ITollAndFineService _tollAndFineService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;

    public IndexModel(
        ITollAndFineService tollAndFineService,
        IVehicleService vehicleService,
        IDriverService driverService)
    {
        _tollAndFineService = tollAndFineService;
        _vehicleService = vehicleService;
        _driverService = driverService;
    }

    public TollsAndFinesDashboardDto Dashboard { get; private set; } = null!;
    public PagedResult<TollTransactionDto> Tolls { get; private set; } = null!;
    public PagedResult<TrafficViolationDto> Violations { get; private set; } = null!;
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public TollSystemType? TollSystemFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public ViolationLiabilityStatus? LiabilityFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _tollAndFineService.GetDashboardAsync(cancellationToken);

        Tolls = await _tollAndFineService.GetTollsPagedAsync(
            new TollFilterRequest(TollSystem: TollSystemFilter, PageNumber: PageNumber, PageSize: 20),
            cancellationToken);

        Violations = await _tollAndFineService.GetViolationsPagedAsync(
            new ViolationFilterRequest(LiabilityStatus: LiabilityFilter, PageNumber: PageNumber, PageSize: 20),
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

    public async Task<IActionResult> OnPostCreateTollAsync(
        TollSystemType tollSystem,
        string tollGateName,
        string tollGateCode,
        decimal amount,
        DateTime transactionTimeUtc,
        Guid vehicleId,
        string? tagNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tollAndFineService.CreateTollAsync(
                new CreateTollTransactionRequest(tollSystem, tollGateName, tollGateCode, amount, transactionTimeUtc, vehicleId, tagNumber),
                cancellationToken);

            var driverMatchMsg = result.MatchedDriverName != null ? $" Automatically matched to driver {result.MatchedDriverName}." : "";
            SuccessMessage = $"Toll transaction of AED {amount:N2} recorded for {result.VehiclePlateNumber}.{driverMatchMsg}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateViolationAsync(
        string ticketNumber,
        string authorityName,
        string violationCode,
        string description,
        decimal fineAmount,
        int blackPoints,
        DateTime violationTimeUtc,
        Guid vehicleId,
        string? location,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tollAndFineService.CreateViolationAsync(
                new CreateTrafficViolationRequest(ticketNumber, authorityName, violationCode, description, fineAmount, blackPoints, violationTimeUtc, vehicleId, location),
                cancellationToken);

            var driverMatchMsg = result.MatchedDriverName != null ? $" Liability auto-assigned to {result.MatchedDriverName}." : "";
            SuccessMessage = $"Traffic violation ticket {ticketNumber} (AED {fineAmount:N2}) ingested.{driverMatchMsg}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignLiabilityAsync(
        Guid violationId,
        Guid driverId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tollAndFineService.AssignLiabilityAsync(
                violationId,
                new AssignViolationLiabilityRequest(driverId),
                cancellationToken);

            SuccessMessage = $"Violation liability assigned to driver {result.MatchedDriverName}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDisputeAsync(
        Guid violationId,
        string disputeReason,
        CancellationToken cancellationToken)
    {
        try
        {
            await _tollAndFineService.DisputeViolationAsync(
                violationId,
                new DisputeViolationRequest(disputeReason),
                cancellationToken);

            SuccessMessage = "Violation dispute logged for police / authority review.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSettleAsync(
        Guid violationId,
        ViolationLiabilityStatus status,
        string? notes,
        CancellationToken cancellationToken)
    {
        try
        {
            await _tollAndFineService.SettleViolationAsync(
                violationId,
                new SettleViolationRequest(status, notes),
                cancellationToken);

            SuccessMessage = $"Violation settled under status {status}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAutoMatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            var count = await _tollAndFineService.AutoMatchDriverLiabilityAsync(cancellationToken);
            SuccessMessage = $"Driver auto-matching completed: {count} toll transactions and traffic violations reconciled against active shift sessions.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
