using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning;

public static class PlanOptimizationMapper
{
    public static PlanOptimizationDto ToDto(
        PlanOptimizationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        PlanScheduleDto MapSchedule(ScheduleEvaluation evaluation)
        {
            return PlanScheduleMapper.ToDto(
                new PlanScheduleResult(
                    result.PlanId,
                    result.Revision,
                    result.TravelMode,
                    result.DepartureAtUtc,
                    evaluation));
        }

        var original = MapSchedule(result.OriginalSchedule);
        var best = result.Optimization.BestSchedule;

        var optimized = best is null
            ? null
            : MapSchedule(best);

        double? timeSavedSeconds = null;

        if (result.OriginalSchedule.IsFeasible &&
            best is { IsFeasible: true })
        {
            timeSavedSeconds = (
                result.OriginalSchedule.FinishedAtUtc!.Value -
                best.FinishedAtUtc!.Value
            ).TotalSeconds;
        }

        return new PlanOptimizationDto(
            original,
            optimized,
            result.Optimization.SearchCompleted,
            result.Optimization.IsOptimal,
            result.Optimization.IsProvenInfeasible,
            result.Optimization.ExploredNodes,
            timeSavedSeconds);
    }
}