using ErrandFlow.Application.Planning;
using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Planning.UseCases;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;
using ErrandFlow.Domain.Planning;
using ErrandFlow.Domain.Plans;
using Xunit;

namespace ErrandFlow.Application.Tests;

public sealed class OptimizePlanTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsNullWhenPlanIsNotAvailable()
    {
        var dependencies = new TestDependencies
        {
            ReturnMissingPlan = true
        };

        var requestedOwnerId = Guid.NewGuid();

        var result = await dependencies.CreateUseCase().ExecuteAsync(
            requestedOwnerId,
            dependencies.Plan.Id,
            TravelMode.Walking);

        Assert.Null(result);
        Assert.Equal(
            requestedOwnerId,
            dependencies.RequestedOwnerId);
        Assert.Equal(
            dependencies.Plan.Id,
            dependencies.RequestedPlanId);
        Assert.Equal(0, dependencies.SnapshotCalls);
        Assert.Equal(0, dependencies.ProviderCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsDifferentSnapshotRevision()
    {
        var dependencies = new TestDependencies();

        dependencies.SnapshotRevision =
            dependencies.Plan.Revision + 1UL;

        var exception = await Assert.ThrowsAsync<PlanConcurrencyException>(
            () => dependencies.CreateUseCase().ExecuteAsync(
                dependencies.Plan.OwnerId,
                dependencies.Plan.Id,
                TravelMode.Walking));

        Assert.Equal(dependencies.Plan.Id, exception.PlanId);
        Assert.Equal(
            dependencies.Plan.Revision,
            exception.ExpectedRevision);
    }

    [Fact]
    public async Task ExecuteAsync_UsesOneGraphAndDoesNotChangePlan()
    {
        var dependencies = new TestDependencies();
        var originalRevision = dependencies.Plan.Revision;

        var originalIds = dependencies.Plan.Errands
            .Select(errand => errand.Id)
            .ToArray();

        using var cancellation = new CancellationTokenSource();

        var result = await dependencies.CreateUseCase().ExecuteAsync(
            dependencies.Plan.OwnerId,
            dependencies.Plan.Id,
            TravelMode.Walking,
            cancellation.Token);

        var actual = Assert.IsType<PlanOptimizationResult>(result);

        Assert.Equal(1, dependencies.SnapshotCalls);
        Assert.Equal(1, dependencies.ProviderCalls);
        Assert.Equal(
            cancellation.Token,
            dependencies.ProviderToken);

        Assert.Equal(originalRevision, actual.Revision);
        Assert.Equal(originalRevision, dependencies.Plan.Revision);

        Assert.Equal(
            originalIds,
            dependencies.Plan.Errands.Select(errand => errand.Id));

        Assert.Equal(
            dependencies.Plan.OwnerId,
            dependencies.RequestedOwnerId);
        Assert.Equal(
            dependencies.Plan.OwnerId,
            dependencies.SnapshotOwnerId);
        Assert.Equal(
            dependencies.Plan.Id,
            dependencies.SnapshotPlanId);

        var expectedDeparture =
            new DateTime(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

        Assert.Equal(expectedDeparture, actual.DepartureAtUtc);
        Assert.True(actual.OriginalSchedule.IsFeasible);
        Assert.Equal(
            expectedDeparture.AddMinutes(15),
            actual.OriginalSchedule.FinishedAtUtc);

        var best = Assert.IsType<ScheduleEvaluation>(
            actual.Optimization.BestSchedule);

        Assert.True(actual.Optimization.IsOptimal);
        Assert.Equal(
            actual.OriginalSchedule.FinishedAtUtc,
            best.FinishedAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesProviderFailure()
    {
        var failure = new InvalidOperationException(
            "Simulated provider failure.");

        var dependencies = new TestDependencies
        {
            ProviderFailure = failure
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dependencies.CreateUseCase().ExecuteAsync(
                dependencies.Plan.OwnerId,
                dependencies.Plan.Id,
                TravelMode.Walking));

        Assert.Same(failure, exception);
        Assert.Equal(1, dependencies.ProviderCalls);
    }
[Fact]
public async Task ExecuteAsync_StopsWhenCancellationIsRequested()
{
    var dependencies = new TestDependencies();

    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(
        () => dependencies.CreateUseCase().ExecuteAsync(
            dependencies.Plan.OwnerId,
            dependencies.Plan.Id,
            TravelMode.Walking,
            cancellation.Token));

    Assert.Equal(0, dependencies.SnapshotCalls);
    Assert.Equal(0, dependencies.ProviderCalls);
}

[Fact]
public async Task ExecuteAsync_RejectsMissingOriginBeforeBuildingGraph()
{
    var dependencies = new TestDependencies();

    dependencies.Plan.SetLocations(null, null);

    var exception = await Assert.ThrowsAsync<ArgumentException>(
        () => dependencies.CreateUseCase().ExecuteAsync(
            dependencies.Plan.OwnerId,
            dependencies.Plan.Id,
            TravelMode.Walking));

    Assert.Equal("originLocationId", exception.ParamName);
    Assert.Equal(0, dependencies.SnapshotCalls);
    Assert.Equal(0, dependencies.ProviderCalls);
}

[Fact]
public async Task ExecuteAsync_RejectsElevenErrandsBeforeBuildingGraph()
{
    var dependencies = new TestDependencies();

    var location = dependencies.Plan.CreateLocation(
        "Service center", null, 39.4720m, -0.3820m);

    for (var index = 0; index < 10; index++)
    {
        dependencies.Plan.AddErrand(
            location,
            $"Additional task {index + 1}",
            1);
    }

    Assert.Equal(11, dependencies.Plan.Errands.Count);

    var exception = await Assert.ThrowsAsync<ArgumentException>(
        () => dependencies.CreateUseCase().ExecuteAsync(
            dependencies.Plan.OwnerId,
            dependencies.Plan.Id,
            TravelMode.Walking));

    Assert.Equal("errands", exception.ParamName);
    Assert.Equal(0, dependencies.SnapshotCalls);
    Assert.Equal(0, dependencies.ProviderCalls);
}
    private sealed class TestDependencies :
        IPlanRepository,
        IPlanRoutingRepository,
        ITravelGraphProvider
    {
        private readonly RoutingLocation[] _locations;
        private readonly LocationGraph _graph;

        public Plan Plan { get; }

        public bool ReturnMissingPlan { get; init; }
        public ulong? SnapshotRevision { get; set; }
        public Exception? ProviderFailure { get; init; }

        public Guid? RequestedOwnerId { get; private set; }
        public Guid? RequestedPlanId { get; private set; }
        public Guid? SnapshotOwnerId { get; private set; }
        public Guid? SnapshotPlanId { get; private set; }

        public int SnapshotCalls { get; private set; }
        public int ProviderCalls { get; private set; }
        public CancellationToken ProviderToken { get; private set; }

        public TestDependencies()
        {
            Plan = new Plan(
                Guid.NewGuid(),
                "Application test",
                new DateOnly(2026, 11, 6),
                new TimeOnly(10, 0),
                "Europe/Madrid");

            var origin = Plan.CreateLocation(
                "Home", null, 39.4700m, -0.3800m);

            var pharmacy = Plan.CreateLocation(
                "Pharmacy", null, 39.4710m, -0.3810m);

            Plan.AddErrand(pharmacy, "Collect medication", 10);
            Plan.SetLocations(origin.Id, null);

            _locations = new[]
            {
                new RoutingLocation(
                    origin.Id, 39.4700m, -0.3800m),
                new RoutingLocation(
                    pharmacy.Id, 39.4710m, -0.3810m)
            };

            var costs =
                new Dictionary<(Guid From, Guid To), TravelCost>
                {
                    [(origin.Id, pharmacy.Id)] =
                        TravelCost.Create(
                            TimeSpan.FromMinutes(5), 400m),
                    [(pharmacy.Id, origin.Id)] =
                        TravelCost.Create(
                            TimeSpan.FromMinutes(8), 600m)
                };

            _graph = new LocationGraph(
                new[] { origin.Id, pharmacy.Id },
                costs);
        }

        public OptimizePlan CreateUseCase()
        {
            return new OptimizePlan(
                this,
                new BuildPlanGraph(this, this));
        }

        public Task<Plan?> GetWithErrandsAsync(
            Guid ownerId,
            Guid planId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RequestedOwnerId = ownerId;
            RequestedPlanId = planId;

            return Task.FromResult<Plan?>(
                ReturnMissingPlan ? null : Plan);
        }

        public Task<PlanRoutingSnapshot?> GetSnapshotAsync(
            Guid ownerId,
            Guid planId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SnapshotCalls++;
            SnapshotOwnerId = ownerId;
            SnapshotPlanId = planId;

            return Task.FromResult<PlanRoutingSnapshot?>(
                new PlanRoutingSnapshot(
                    Plan.Id,
                    SnapshotRevision ?? Plan.Revision,
                    Plan.OriginLocationId,
                    Plan.DestinationLocationId,
                    _locations));
        }

        public Task<LocationGraph> BuildAsync(
            IReadOnlyList<RoutingLocation> locations,
            TravelMode travelMode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ProviderCalls++;
            ProviderToken = cancellationToken;

            Assert.Equal(TravelMode.Walking, travelMode);

            Assert.Equal(
                _locations.Select(location => location.LocationId),
                locations.Select(location => location.LocationId));

            if (ProviderFailure is not null)
            {
                return Task.FromException<LocationGraph>(
                    ProviderFailure);
            }

            return Task.FromResult(_graph);
        }

        public Task<IReadOnlyList<Plan>> GetByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Plan?> GetByIdAsync(
            Guid ownerId,
            Guid planId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AddAsync(
            Plan plan,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void Remove(Plan plan)
            => throw new NotSupportedException();

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            Guid ownerId,
            Guid planId,
            ulong expectedRevision,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}