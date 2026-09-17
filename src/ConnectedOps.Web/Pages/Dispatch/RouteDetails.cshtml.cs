using ConnectedOps.Application.Dispatch;
using ConnectedOps.Domain.Dispatch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Dispatch;

[Authorize]
public class RouteDetailsModel : PageModel
{
    private readonly IDispatchService _dispatchService;

    public RouteDetailsModel(IDispatchService dispatchService)
    {
        _dispatchService = dispatchService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public DispatchRouteDto Route { get; private set; } = null!;
    public IReadOnlyCollection<DispatchJobDto> AvailableJobs { get; private set; } = [];

    [BindProperty]
    public AddStopInput NewStop { get; set; } = new();

    public class AddStopInput
    {
        public Guid? JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; } = 25.2048;
        public double Longitude { get; set; } = 55.2708;
        public decimal? WeightKg { get; set; }
        public int ServiceDurationMinutes { get; set; } = 15;
        public string? Notes { get; set; }
    }

    [BindProperty]
    public PodInput PodForm { get; set; } = new();

    public class PodInput
    {
        public Guid JobId { get; set; }
        public Guid StopId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public PodVerificationType VerificationType { get; set; } = PodVerificationType.Signature;
        public string? SignatureData { get; set; }
        public string? Notes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage("/Dispatch/Routes");
        }

        var route = await _dispatchService.GetRouteByIdAsync(Id, cancellationToken);
        if (route == null)
        {
            return NotFound();
        }

        Route = route;

        // Load unassigned jobs that can be added as stops to this route
        var unassignedJobs = await _dispatchService.GetJobsPagedAsync(new DispatchJobFilterRequest
        {
            Status = DispatchJobStatus.Unassigned,
            PageNumber = 1,
            PageSize = 50
        }, cancellationToken);

        AvailableJobs = unassignedJobs.Items;
        return Page();
    }

    public async Task<IActionResult> OnPostAddStopAsync(CancellationToken cancellationToken)
    {
        try
        {
            Guid targetJobId;

            if (NewStop.JobId.HasValue && NewStop.JobId.Value != Guid.Empty)
            {
                targetJobId = NewStop.JobId.Value;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(NewStop.CustomerName) || string.IsNullOrWhiteSpace(NewStop.Address))
                {
                    ErrorMessage = "Customer name and address are required to add a stop.";
                    return RedirectToPage(new { id = Id });
                }

                // Create a job first
                var createdJob = await _dispatchService.CreateJobAsync(new CreateDispatchJobRequest
                {
                    Title = string.IsNullOrWhiteSpace(NewStop.Title) ? $"Delivery for {NewStop.CustomerName}" : NewStop.Title.Trim(),
                    CustomerName = NewStop.CustomerName.Trim(),
                    CustomerPhone = string.IsNullOrWhiteSpace(NewStop.CustomerPhone) ? "N/A" : NewStop.CustomerPhone.Trim(),
                    Address = NewStop.Address.Trim(),
                    Latitude = NewStop.Latitude,
                    Longitude = NewStop.Longitude,
                    WeightKg = NewStop.WeightKg,
                    ServiceDurationMinutes = NewStop.ServiceDurationMinutes,
                    SpecialInstructions = NewStop.Notes?.Trim()
                }, cancellationToken);

                targetJobId = createdJob.Id;
            }

            var currentRoute = await _dispatchService.GetRouteByIdAsync(Id, cancellationToken);
            var nextSeq = (currentRoute?.Stops.Count ?? 0) + 1;

            var req = new AddRouteStopRequest
            {
                JobId = targetJobId,
                SequenceOrder = nextSeq,
                Notes = NewStop.Notes?.Trim()
            };

            await _dispatchService.AddStopAsync(Id, req, cancellationToken);
            SuccessMessage = "Stop added to route successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add stop: {ex.Message}";
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostOptimizeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var route = await _dispatchService.GetRouteByIdAsync(Id, cancellationToken);
            double? depotLat = null;
            double? depotLng = null;

            if (route != null && route.Stops.Count > 0)
            {
                depotLat = route.Stops[0].Latitude;
                depotLng = route.Stops[0].Longitude;
            }

            var updated = await _dispatchService.OptimizeRouteAsync(Id, new OptimizeRouteStopsRequest
            {
                DepotLatitude = depotLat,
                DepotLongitude = depotLng
            }, cancellationToken);

            SuccessMessage = $"Route stop sequence optimized using Nearest-Neighbor TSP! Total distance: {updated.EstimatedDistanceKm:F1} km.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to optimize route: {ex.Message}";
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid stopId, RouteStopStatus status, string? notes, CancellationToken cancellationToken)
    {
        try
        {
            var req = new UpdateRouteStopStatusRequest
            {
                Status = status,
                ReasonOrNotes = notes
            };
            await _dispatchService.UpdateStopStatusAsync(stopId, req, cancellationToken);
            SuccessMessage = $"Stop status updated to {status}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update stop: {ex.Message}";
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRecordPodAsync(CancellationToken cancellationToken)
    {
        if (PodForm.JobId == Guid.Empty || string.IsNullOrWhiteSpace(PodForm.RecipientName))
        {
            ErrorMessage = "Recipient name is required for proof of delivery.";
            return RedirectToPage(new { id = Id });
        }

        try
        {
            var req = new RecordProofOfDeliveryRequest
            {
                RecipientName = PodForm.RecipientName.Trim(),
                VerificationType = PodForm.VerificationType,
                SignatureData = PodForm.SignatureData,
                Notes = PodForm.Notes?.Trim(),
                Latitude = PodForm.Latitude,
                Longitude = PodForm.Longitude
            };

            await _dispatchService.RecordProofOfDeliveryAsync(PodForm.JobId, req, cancellationToken);
            SuccessMessage = $"Proof of Delivery verified for {PodForm.RecipientName} and stop completed successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to record POD: {ex.Message}";
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveStopAsync(Guid stopId, CancellationToken cancellationToken)
    {
        try
        {
            await _dispatchService.RemoveStopAsync(Id, stopId, cancellationToken);
            SuccessMessage = "Stop removed from route.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to remove stop: {ex.Message}";
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostStartRouteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dispatchService.StartRouteAsync(Id, cancellationToken);
            SuccessMessage = "Route is now In Progress.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start route: {ex.Message}";
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostCompleteRouteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dispatchService.CompleteRouteAsync(Id, null, cancellationToken);
            SuccessMessage = "Route has been marked as Completed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to complete route: {ex.Message}";
        }

        return RedirectToPage(new { id = Id });
    }
}
