namespace ErrandFlow.Application.Planning.Dtos;

public sealed record PlanOptimizationDto(
    PlanScheduleDto Original,
    PlanScheduleDto? Optimized,
    bool SearchCompleted,
    bool IsOptimal,
    bool IsProvenInfeasible,
    int ExploredNodes,
    double? TimeSavedSeconds);