using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Dispatch;
using ConnectedOps.Domain.Dispatch;
using ConnectedOps.Infrastructure.Dispatch;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class DispatchServiceTests
{
    [Fact]
    public async Task TenantIsolation_EnforcesSeparation_BetweenTenants()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var contextA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var contextB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };

        var serviceA = new DispatchService(db, contextA);
        var serviceB = new DispatchService(db, contextB);

        // Tenant A creates a job
        var jobA = await serviceA.CreateJobAsync(new CreateDispatchJobRequest
        {
            Title = "Delivery to Tenant A Customer",
            CustomerName = "Alice",
            CustomerPhone = "+123456789",
            Address = "100 A Street",
            Latitude = 25.2048,
            Longitude = 55.2708
        });

        // Tenant A creates a route
        var routeA = await serviceA.CreateRouteAsync(new CreateDispatchRouteRequest
        {
            Name = "Tenant A Route 1",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        // Tenant B lists jobs and routes
        var bJobs = await serviceB.GetJobsPagedAsync(new DispatchJobFilterRequest());
        var bRoutes = await serviceB.GetRoutesPagedAsync(new DispatchRouteFilterRequest());

        Assert.DoesNotContain(bJobs.Items, j => j.Id == jobA.Id);
        Assert.DoesNotContain(bRoutes.Items, r => r.Id == routeA.Id);

        // Tenant B cannot fetch by ID
        var fetchJob = await serviceB.GetJobByIdAsync(jobA.Id);
        Assert.Null(fetchJob);

        var fetchRoute = await serviceB.GetRouteByIdAsync(routeA.Id);
        Assert.Null(fetchRoute);
    }

    [Fact]
    public async Task Job_Create_Update_Cancel_Lifecycle()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new DispatchService(db, userContext);

        // 1. Create
        var createReq = new CreateDispatchJobRequest
        {
            Title = "Industrial Supplies Delivery",
            JobType = DispatchJobType.Delivery,
            Priority = DispatchJobPriority.High,
            CustomerName = "Atlas Heavy Industries",
            CustomerPhone = "+971 4 888 1234",
            CustomerEmail = "logistics@atlas.com",
            Address = "Jebel Ali Free Zone, Gate 4",
            Latitude = 24.9857,
            Longitude = 55.0867,
            WeightKg = 450.5m,
            VolumeM3 = 2.4m,
            PackageCount = 4,
            ServiceDurationMinutes = 30,
            SpecialInstructions = "Safety gear required on site"
        };

        var job = await service.CreateJobAsync(createReq);

        Assert.NotNull(job);
        Assert.Equal("Industrial Supplies Delivery", job.Title);
        Assert.Equal(DispatchJobStatus.Unassigned, job.Status);
        Assert.Equal(DispatchJobPriority.High, job.Priority);
        Assert.Equal(450.5m, job.WeightKg);
        Assert.StartsWith("JOB-", job.JobNumber);

        // 2. Update
        var updateReq = new UpdateDispatchJobRequest
        {
            Title = "Industrial Supplies Delivery - Priority 1",
            JobType = DispatchJobType.Delivery,
            Priority = DispatchJobPriority.Urgent,
            CustomerName = "Atlas Heavy Industries",
            CustomerPhone = "+971 4 888 1234",
            CustomerEmail = "logistics@atlas.com",
            Address = "Jebel Ali Free Zone, Gate 5 (Updated)",
            Latitude = 24.9860,
            Longitude = 55.0870,
            WeightKg = 500m,
            VolumeM3 = 2.5m,
            PackageCount = 5,
            ServiceDurationMinutes = 40,
            SpecialInstructions = "Deliver to Warehouse 12"
        };

        var updated = await service.UpdateJobAsync(job.Id, updateReq);
        Assert.Equal("Industrial Supplies Delivery - Priority 1", updated.Title);
        Assert.Equal(DispatchJobPriority.Urgent, updated.Priority);
        Assert.Equal(500m, updated.WeightKg);

        // 3. Cancel
        var cancelled = await service.CancelJobAsync(job.Id, "Customer requested postponement");
        Assert.True(cancelled);

        var retrieved = await service.GetJobByIdAsync(job.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(DispatchJobStatus.Cancelled, retrieved.Status);
    }

    [Fact]
    public async Task Route_AddAndRemoveStops_RecalculatesDistanceAndOrder()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new DispatchService(db, userContext);

        // Create Route
        var route = await service.CreateRouteAsync(new CreateDispatchRouteRequest
        {
            Name = "Downtown Courier Run",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        // Create 2 jobs
        var job1 = await service.CreateJobAsync(new CreateDispatchJobRequest
        {
            Title = "Stop 1 Delivery",
            CustomerName = "Burj Customer",
            Address = "Downtown Dubai, Blvd 1",
            Latitude = 25.1972,
            Longitude = 55.2744
        });

        var job2 = await service.CreateJobAsync(new CreateDispatchJobRequest
        {
            Title = "Stop 2 Delivery",
            CustomerName = "Business Bay Customer",
            Address = "Business Bay, Tower 2",
            Latitude = 25.1857,
            Longitude = 55.2630
        });

        // Add Stop 1
        var withStop1 = await service.AddStopAsync(route.Id, new AddRouteStopRequest
        {
            JobId = job1.Id,
            SequenceOrder = 1,
            Notes = "First morning stop"
        });

        Assert.Single(withStop1.Stops);
        Assert.Equal(1, withStop1.Stops[0].SequenceOrder);

        // Add Stop 2
        var withStop2 = await service.AddStopAsync(route.Id, new AddRouteStopRequest
        {
            JobId = job2.Id,
            SequenceOrder = 2
        });

        Assert.Equal(2, withStop2.Stops.Count);
        Assert.True(withStop2.EstimatedDistanceKm > 0);

        // Remove Stop 1
        var stop1Id = withStop2.Stops.First(s => s.JobId == job1.Id).Id;
        var withRemoved = await service.RemoveStopAsync(route.Id, stop1Id);

        Assert.Single(withRemoved.Stops);
        Assert.Equal(job2.Id, withRemoved.Stops[0].JobId);
        Assert.Equal(1, withRemoved.Stops[0].SequenceOrder); // resequenced
    }

    [Fact]
    public async Task Route_Optimization_NearestNeighborTSP_ReordersSequences()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new DispatchService(db, userContext);

        var route = await service.CreateRouteAsync(new CreateDispatchRouteRequest
        {
            Name = "Optimization Test Route",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        // Create 3 jobs at known coordinates in ascending distance from depot (25.2000, 55.2700)
        // Job A: near (25.2010, 55.2710)
        // Job B: mid (25.2100, 55.2800)
        // Job C: far (25.3000, 55.3500)
        var jobFar = await service.CreateJobAsync(new CreateDispatchJobRequest
        {
            Title = "Far Stop",
            CustomerName = "Far Customer",
            Address = "Sharjah Border",
            Latitude = 25.3000,
            Longitude = 55.3500
        });

        var jobNear = await service.CreateJobAsync(new CreateDispatchJobRequest
        {
            Title = "Near Stop",
            CustomerName = "Near Customer",
            Address = "Close Depot Hub",
            Latitude = 25.2010,
            Longitude = 55.2710
        });

        var jobMid = await service.CreateJobAsync(new CreateDispatchJobRequest
        {
            Title = "Mid Stop",
            CustomerName = "Mid Customer",
            Address = "Midway Center",
            Latitude = 25.2100,
            Longitude = 55.2800
        });

        // Add them in backwards order: Far, Mid, Near
        await service.AddStopAsync(route.Id, new AddRouteStopRequest { JobId = jobFar.Id, SequenceOrder = 1 });
        await service.AddStopAsync(route.Id, new AddRouteStopRequest { JobId = jobMid.Id, SequenceOrder = 2 });
        await service.AddStopAsync(route.Id, new AddRouteStopRequest { JobId = jobNear.Id, SequenceOrder = 3 });

        // Optimize from depot near JobNear
        var optimized = await service.OptimizeRouteAsync(route.Id, new OptimizeRouteStopsRequest
        {
            DepotLatitude = 25.2000,
            DepotLongitude = 55.2700
        });

        Assert.Equal(3, optimized.Stops.Count);
        // The nearest stop to (25.2000, 55.2700) should now be first!
        Assert.Equal(jobNear.Id, optimized.Stops[0].JobId);
        Assert.Equal(1, optimized.Stops[0].SequenceOrder);
        Assert.Equal(2, optimized.Stops[1].SequenceOrder);
        Assert.Equal(3, optimized.Stops[2].SequenceOrder);
    }

    [Fact]
    public async Task Route_Lifecycle_Dispatch_Start_Complete()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new DispatchService(db, userContext);

        var route = await service.CreateRouteAsync(new CreateDispatchRouteRequest
        {
            Name = "Route Lifecycle Test",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        Assert.Equal(DispatchRouteStatus.Draft, route.Status);

        // Dispatch
        var dispatched = await service.DispatchRouteAsync(route.Id);
        Assert.True(dispatched);
        var r1 = await service.GetRouteByIdAsync(route.Id);
        Assert.Equal(DispatchRouteStatus.Dispatched, r1!.Status);

        // Start
        var started = await service.StartRouteAsync(route.Id);
        Assert.True(started);
        var r2 = await service.GetRouteByIdAsync(route.Id);
        Assert.Equal(DispatchRouteStatus.InProgress, r2!.Status);
        Assert.NotNull(r2.StartedAtUtc);

        // Complete
        var completed = await service.CompleteRouteAsync(route.Id, 42.5m);
        Assert.True(completed);
        var r3 = await service.GetRouteByIdAsync(route.Id);
        Assert.Equal(DispatchRouteStatus.Completed, r3!.Status);
        Assert.NotNull(r3.CompletedAtUtc);
        Assert.Equal(42.5m, r3.ActualDistanceKm);
    }

    [Fact]
    public async Task ProofOfDelivery_Recording_UpdatesJobAndStopStatus()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new DispatchService(db, userContext);

        var route = await service.CreateRouteAsync(new CreateDispatchRouteRequest
        {
            Name = "POD Route",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        var job = await service.CreateJobAsync(new CreateDispatchJobRequest
        {
            Title = "Delivery with Signature",
            CustomerName = "John Doe",
            Address = "Marina Mall Level 1",
            Latitude = 25.0763,
            Longitude = 55.1403
        });

        var routeWithStop = await service.AddStopAsync(route.Id, new AddRouteStopRequest
        {
            JobId = job.Id,
            SequenceOrder = 1
        });

        var stopId = routeWithStop.Stops[0].Id;

        // Record POD
        var podReq = new RecordProofOfDeliveryRequest
        {
            RecipientName = "John Doe",
            VerificationType = PodVerificationType.Signature,
            SignatureData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
            Notes = "Received in good order by front desk",
            Latitude = 25.0763,
            Longitude = 55.1403
        };

        var pod = await service.RecordProofOfDeliveryAsync(job.Id, podReq);

        Assert.NotNull(pod);
        Assert.Equal("John Doe", pod.RecipientName);
        Assert.Equal(PodVerificationType.Signature, pod.VerificationType);
        Assert.NotNull(pod.SignatureData);

        // Verify Job is marked Completed
        var updatedJob = await service.GetJobByIdAsync(job.Id);
        Assert.NotNull(updatedJob);
        Assert.Equal(DispatchJobStatus.Completed, updatedJob.Status);
        Assert.NotNull(updatedJob.CompletedAtUtc);

        // Verify Route Stop is marked Completed and HasPod is true
        var updatedRoute = await service.GetRouteByIdAsync(route.Id);
        Assert.NotNull(updatedRoute);
        var stop = updatedRoute.Stops.First(s => s.Id == stopId);
        Assert.Equal(RouteStopStatus.Completed, stop.Status);
        Assert.True(stop.HasPod);

        // Verify POD lookup
        var fetchedPod = await service.GetProofOfDeliveryByJobIdAsync(job.Id);
        Assert.NotNull(fetchedPod);
        Assert.Equal("John Doe", fetchedPod.RecipientName);
    }

    [Fact]
    public async Task Stop_FailureStatus_RecordsReason()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new DispatchService(db, userContext);

        var route = await service.CreateRouteAsync(new CreateDispatchRouteRequest
        {
            Name = "Failed Stop Route",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        var job = await service.CreateJobAsync(new CreateDispatchJobRequest
        {
            Title = "Delivery to Closed Office",
            CustomerName = "Office Tower",
            Address = "DIFC Gate 2",
            Latitude = 25.2100,
            Longitude = 55.2800
        });

        var routeWithStop = await service.AddStopAsync(route.Id, new AddRouteStopRequest
        {
            JobId = job.Id,
            SequenceOrder = 1
        });

        var stopId = routeWithStop.Stops[0].Id;

        // Update Stop to Failed
        var updatedStop = await service.UpdateStopStatusAsync(stopId, new UpdateRouteStopStatusRequest
        {
            Status = RouteStopStatus.Failed,
            ReasonOrNotes = "Premises closed, security refused entry"
        });

        Assert.Equal(RouteStopStatus.Failed, updatedStop.Status);

        // Verify linked job is also updated to Failed
        var updatedJob = await service.GetJobByIdAsync(job.Id);
        Assert.NotNull(updatedJob);
        Assert.Equal(DispatchJobStatus.Failed, updatedJob.Status);
        Assert.Equal("Premises closed, security refused entry", updatedJob.FailureReason);
    }
}
