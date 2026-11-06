using ErrandFlow.Domain.Errands;

namespace ErrandFlow.Domain.Planning;

public static class OriginalOrderEvaluator
{
    public static ScheduleEvaluation Evaluate(
        Guid? originLocationId,
        Guid? destinationLocationId,
        DateTime departureAtUtc,
        IReadOnlyCollection<Errand> errands,
        LocationGraph graph)
    {
        ArgumentNullException.ThrowIfNull(errands);
        ArgumentNullException.ThrowIfNull(graph);

        if (originLocationId is not Guid originId ||
            originId == Guid.Empty)
        {
            throw new ArgumentException(
                "An origin location is required.",
                nameof(originLocationId));
        }

        if (departureAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Departure must be expressed in UTC.",
                nameof(departureAtUtc));
        }

        if (destinationLocationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Destination location ID cannot be empty.",
                nameof(destinationLocationId));
        }

        var orderedErrands = errands
            .OrderBy(errand => errand.OriginalPosition)
            .ToArray();

        if (orderedErrands.Select(errand => errand.Id)
                .Distinct().Count() != orderedErrands.Length ||
            orderedErrands.Select(errand => errand.OriginalPosition)
                .Distinct().Count() != orderedErrands.Length)
        {
            throw new ArgumentException(
                "Errand IDs and original positions must be unique.",
                nameof(errands));
        }

        var graphLocations = graph.LocationIds.ToHashSet();

        if (!graphLocations.Contains(originId) ||
            (destinationLocationId is Guid destinationId &&
             !graphLocations.Contains(destinationId)) ||
            orderedErrands.Any(errand =>
                !graphLocations.Contains(errand.LocationId)))
        {
            throw new ArgumentException(
                "The graph must contain every required location.",
                nameof(graph));
        }

        var scheduled = new List<ScheduledErrand>();
        var currentLocationId = originId;
        var currentTime = departureAtUtc;

        ScheduleEvaluation Fail(
            string code,
            Guid? errandId,
            Guid toLocationId)
        {
            return new ScheduleEvaluation(
                scheduled.AsReadOnly(),
                null,
                new ScheduleConflict(
                    code,
                    errandId,
                    currentLocationId,
                    toLocationId));
        }

        foreach (var errand in orderedErrands)
        {
            var travel = graph.GetCost(
                currentLocationId,
                errand.LocationId);

            if (!travel.IsReachable)
            {
                return Fail(
                    "unreachable_errand",
                    errand.Id,
                    errand.LocationId);
            }

            var travelDuration = travel.Duration!.Value;
            var arrival = currentTime.Add(travelDuration);
            var start = arrival;

            if (errand.EarliestStartAt is DateTime earliest &&
                start < earliest)
            {
                start = earliest;
            }

            if (errand.FixedStartAt is DateTime fixedStart)
            {
                if (start > fixedStart)
                {
                    return Fail(
                        "fixed_start_missed",
                        errand.Id,
                        errand.LocationId);
                }

                start = fixedStart;
            }

            var finish = start.AddMinutes(errand.DurationMinutes);

            if (errand.LatestFinishAt is DateTime latestFinish &&
                finish > latestFinish)
            {
                return Fail(
                    "latest_finish_exceeded",
                    errand.Id,
                    errand.LocationId);
            }

            scheduled.Add(new ScheduledErrand(
                errand.Id,
                errand.LocationId,
                arrival,
                start,
                finish,
                travelDuration,
                start - arrival));

            currentLocationId = errand.LocationId;
            currentTime = finish;
        }

        if (destinationLocationId is Guid finalLocationId)
        {
            var finalTravel = graph.GetCost(
                currentLocationId,
                finalLocationId);

            if (!finalTravel.IsReachable)
            {
                return Fail(
                    "unreachable_destination",
                    null,
                    finalLocationId);
            }

            currentTime = currentTime.Add(
                finalTravel.Duration!.Value);
        }

        return new ScheduleEvaluation(
            scheduled.AsReadOnly(),
            currentTime,
            null);
    }
}