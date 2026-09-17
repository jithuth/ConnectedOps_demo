using ConnectedOps.Application.Dispatch;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Dispatch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Dispatch;

[Authorize]
public class JobsModel : PageModel
{
    private readonly IDispatchService _dispatchService;

    public JobsModel(IDispatchService dispatchService)
    {
        _dispatchService = dispatchService;
    }

    [BindProperty(SupportsGet = true)]
    public DispatchJobStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DispatchJobPriority? Priority { get; set; }

    [BindProperty(SupportsGet = true)]
    public DispatchJobType? JobType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<DispatchJobDto> Jobs { get; private set; } = null!;

    [BindProperty]
    public CreateJobInput CreateInput { get; set; } = new();

    public class CreateJobInput
    {
        public string Title { get; set; } = string.Empty;
        public DispatchJobType JobType { get; set; } = DispatchJobType.Delivery;
        public DispatchJobPriority Priority { get; set; } = DispatchJobPriority.Standard;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; } = 25.2048;
        public double Longitude { get; set; } = 55.2708;
        public decimal? WeightKg { get; set; }
        public decimal? VolumeM3 { get; set; }
        public int PackageCount { get; set; } = 1;
        public DateTime? TimeWindowStartUtc { get; set; }
        public DateTime? TimeWindowEndUtc { get; set; }
        public int ServiceDurationMinutes { get; set; } = 15;
        public string? SpecialInstructions { get; set; }
    }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var filter = new DispatchJobFilterRequest
        {
            SearchTerm = SearchTerm,
            Status = Status,
            JobType = JobType,
            Priority = Priority,
            PageNumber = PageNumber,
            PageSize = 15
        };

        Jobs = await _dispatchService.GetJobsPagedAsync(filter, cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CreateInput.CustomerName) || string.IsNullOrWhiteSpace(CreateInput.Address))
        {
            ErrorMessage = "Customer name and delivery address are required.";
            return RedirectToPage();
        }

        var title = string.IsNullOrWhiteSpace(CreateInput.Title)
            ? $"{CreateInput.JobType} for {CreateInput.CustomerName}"
            : CreateInput.Title.Trim();

        try
        {
            var req = new CreateDispatchJobRequest
            {
                Title = title,
                JobType = CreateInput.JobType,
                Priority = CreateInput.Priority,
                CustomerName = CreateInput.CustomerName.Trim(),
                CustomerPhone = CreateInput.CustomerPhone?.Trim() ?? string.Empty,
                CustomerEmail = CreateInput.CustomerEmail?.Trim(),
                Address = CreateInput.Address.Trim(),
                Latitude = CreateInput.Latitude,
                Longitude = CreateInput.Longitude,
                WeightKg = CreateInput.WeightKg,
                VolumeM3 = CreateInput.VolumeM3,
                PackageCount = CreateInput.PackageCount,
                TimeWindowStartUtc = CreateInput.TimeWindowStartUtc,
                TimeWindowEndUtc = CreateInput.TimeWindowEndUtc,
                ServiceDurationMinutes = CreateInput.ServiceDurationMinutes,
                SpecialInstructions = CreateInput.SpecialInstructions?.Trim()
            };

            var created = await _dispatchService.CreateJobAsync(req, cancellationToken);
            SuccessMessage = $"Job {created.JobNumber} created successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create job: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid jobId, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            await _dispatchService.CancelJobAsync(jobId, reason ?? "Cancelled by dispatcher", cancellationToken);
            SuccessMessage = "Job was cancelled successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to cancel job: {ex.Message}";
        }

        return RedirectToPage();
    }
}
