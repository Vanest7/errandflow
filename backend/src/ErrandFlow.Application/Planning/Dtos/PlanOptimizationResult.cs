using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning.Dtos;

public sealed record PlanOptimizationResult(
    Guid PlanId,
    ulong Revision,
    TravelMode TravelMode,
    DateTime DepartureAtUtc,
    ScheduleEvaluation OriginalSchedule,
    ScheduleOptimizationResult Optimization);