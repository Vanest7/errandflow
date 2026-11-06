namespace ErrandFlow.Application.Planning.Dtos;

public sealed record OptimizedPlanRouteDto(
    PlanOptimizationDto Planning,
    RoutePathDto? Path,
    PlanScheduleDto? RouteSchedule);