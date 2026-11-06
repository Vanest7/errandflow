using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning.Dtos;

public sealed record PlanScheduleResult(
    Guid PlanId,
    ulong Revision,
    TravelMode TravelMode,
    DateTime DepartureAtUtc,
    ScheduleEvaluation Evaluation);