using ErrandFlow.Application.Plans.Dtos;
using ErrandFlow.Domain.Plans;

namespace ErrandFlow.Application.Plans;

internal static class PlanMapper
{
    public static PlanDto ToDto(Plan plan) =>
        new(
            plan.Id,
            plan.OwnerId,
            plan.Name,
            plan.PlanDate,
            plan.DepartureTime,
            plan.TimeZone,
            plan.OriginLocationId,
            plan.DestinationLocationId,
            plan.Revision,
            plan.CreatedAt,
            plan.UpdatedAt);

    public static PlanSummaryDto ToSummaryDto(Plan plan) =>
        new(
            plan.Id,
            plan.Name,
            plan.PlanDate,
            plan.DepartureTime,
            plan.TimeZone);
}
