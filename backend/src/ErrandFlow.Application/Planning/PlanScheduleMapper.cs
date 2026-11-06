using ErrandFlow.Application.Planning.Dtos;

namespace ErrandFlow.Application.Planning;

public static class PlanScheduleMapper
{
    public static PlanScheduleDto ToDto(PlanScheduleResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var evaluation = result.Evaluation;

        var errands = evaluation.ScheduledErrands
            .Select(errand => new ScheduledErrandDto(
                errand.ErrandId,
                errand.LocationId,
                errand.ArrivalAtUtc,
                errand.StartAtUtc,
                errand.FinishAtUtc,
                errand.TravelDuration.TotalSeconds,
                errand.WaitingDuration.TotalSeconds))
            .ToArray();

        var conflict = evaluation.Conflict;

        return new PlanScheduleDto(
            result.PlanId,
            result.Revision,
            result.TravelMode.ToString(),
            result.DepartureAtUtc,
            evaluation.IsFeasible,
            evaluation.FinishedAtUtc,
            errands,
            conflict is null
                ? null
                : new ScheduleConflictDto(
                    conflict.Code,
                    conflict.ErrandId,
                    conflict.FromLocationId,
                    conflict.ToLocationId));
    }
}