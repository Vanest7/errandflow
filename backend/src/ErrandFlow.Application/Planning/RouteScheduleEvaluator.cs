using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Planning.Exceptions;
using ErrandFlow.Domain.Errands;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning;

public static class RouteScheduleEvaluator
{
    public static ScheduleEvaluation Evaluate(
        Guid originLocationId,
        Guid? destinationLocationId,
        DateTime departureAtUtc,
        IReadOnlyList<Errand> orderedErrands,
        RoutePathDto path)
    {
        ArgumentNullException.ThrowIfNull(orderedErrands);
        ArgumentNullException.ThrowIfNull(path);

        if (originLocationId == Guid.Empty)
        {
            throw new ArgumentException(
                "An origin location is required.",
                nameof(originLocationId));
        }

        if (destinationLocationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Destination location ID cannot be empty.",
                nameof(destinationLocationId));
        }

        if (departureAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Departure must be expressed in UTC.",
                nameof(departureAtUtc));
        }

        if (orderedErrands.Select(errand => errand.Id)
                .Distinct().Count() != orderedErrands.Count ||
            orderedErrands.Select(errand => errand.OriginalPosition)
                .Distinct().Count() != orderedErrands.Count)
        {
            throw new ArgumentException(
                "Errand IDs and original positions must be unique.",
                nameof(orderedErrands));
        }

        var expectedConnections = new List<(Guid From, Guid To)>();
        var previousLocationId = originLocationId;

        void AddConnection(Guid locationId)
        {
            if (previousLocationId != locationId)
            {
                expectedConnections.Add(
                    (previousLocationId, locationId));
            }

            previousLocationId = locationId;
        }

        foreach (var errand in orderedErrands)
        {
            AddConnection(errand.LocationId);
        }

        if (destinationLocationId is Guid destinationId)
        {
            AddConnection(destinationId);
        }

        if (path.Legs is null ||
            path.Legs.Count != expectedConnections.Count)
        {
            throw new RoutePathUnavailableException();
        }

        var costs = new List<TravelCost>(path.Legs.Count);

        for (var index = 0; index < path.Legs.Count; index++)
        {
            var leg = path.Legs[index];
            var expected = expectedConnections[index];

            if (leg is null ||
                leg.FromLocationId != expected.From ||
                leg.ToLocationId != expected.To ||
                !double.IsFinite(leg.TravelDurationSeconds) ||
                !double.IsFinite(leg.DistanceMeters) ||
                leg.TravelDurationSeconds < 0 ||
                leg.DistanceMeters < 0 ||
                leg.TravelDurationSeconds >= TimeSpan.MaxValue.TotalSeconds ||
                leg.DistanceMeters >= (double)decimal.MaxValue)
            {
                throw new RoutePathUnavailableException();
            }

            costs.Add(TravelCost.Create(
                TimeSpan.FromSeconds(leg.TravelDurationSeconds),
                (decimal)leg.DistanceMeters));
        }

        var scheduled = new List<ScheduledErrand>();
        var currentLocationId = originLocationId;
        var currentTime = departureAtUtc;
        var legIndex = 0;

        LocationGraph CreateStepGraph(Guid nextLocationId)
        {
            if (currentLocationId == nextLocationId)
            {
                return new LocationGraph(
                    new[] { currentLocationId },
                    new Dictionary<(Guid From, Guid To), TravelCost>());
            }

            var cost = costs[legIndex++];

            return new LocationGraph(
                new[] { currentLocationId, nextLocationId },
                new Dictionary<(Guid From, Guid To), TravelCost>
                {
                    [(currentLocationId, nextLocationId)] = cost,
                    [(nextLocationId, currentLocationId)] =
                        TravelCost.Unreachable
                });
        }

        foreach (var errand in orderedErrands)
        {
            var step = OrderedScheduleEvaluator.Evaluate(
                currentLocationId,
                null,
                currentTime,
                new[] { errand },
                CreateStepGraph(errand.LocationId));

            if (!step.IsFeasible)
            {
                return new ScheduleEvaluation(
                    scheduled.AsReadOnly(),
                    null,
                    step.Conflict);
            }

            scheduled.AddRange(step.ScheduledErrands);
            currentLocationId = errand.LocationId;
            currentTime = step.FinishedAtUtc!.Value;
        }

        if (destinationLocationId is Guid finalLocationId)
        {
            var finalStep = OrderedScheduleEvaluator.Evaluate(
                currentLocationId,
                finalLocationId,
                currentTime,
                Array.Empty<Errand>(),
                CreateStepGraph(finalLocationId));

            if (!finalStep.IsFeasible)
            {
                return new ScheduleEvaluation(
                    scheduled.AsReadOnly(),
                    null,
                    finalStep.Conflict);
            }

            currentTime = finalStep.FinishedAtUtc!.Value;
        }

        return new ScheduleEvaluation(
            scheduled.AsReadOnly(),
            currentTime,
            null);
    }
}