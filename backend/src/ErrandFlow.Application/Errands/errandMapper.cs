using ErrandFlow.Application.Errands.Dtos;
using ErrandFlow.Domain.Errands;

namespace ErrandFlow.Application.Errands;

internal static class ErrandMapper
{
    public static ErrandDto ToDto(Errand errand)
    {
        return new ErrandDto(
            errand.Id,
            errand.PlanId,
            errand.LocationId,
            errand.Title,
            errand.DurationMinutes,
            errand.OriginalPosition,
            errand.EarliestStartAt,
            errand.LatestFinishAt,
            errand.FixedStartAt,
            errand.CreatedAt,
            errand.UpdatedAt);
    }
}