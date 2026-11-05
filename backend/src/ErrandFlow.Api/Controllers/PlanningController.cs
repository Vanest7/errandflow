using System.Security.Claims;
using ErrandFlow.Application.Planning;
using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Planning.UseCases;
using ErrandFlow.Domain.Planning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErrandFlow.Api.Controllers;

[ApiController]
[Route("api/plans/{planId:guid}/planning")]
[Authorize(Policy = "RegisteredUser")]
public sealed class PlanningController : ControllerBase
{
    private readonly BuildPlanGraph _buildPlanGraph;

    public PlanningController(BuildPlanGraph buildPlanGraph)
    {
        _buildPlanGraph = buildPlanGraph;
    }

    [HttpGet("graph")]
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<PlanGraphDto>> GetGraphAsync(
        Guid planId,
        CancellationToken cancellationToken,
        [FromQuery] TravelMode travelMode = TravelMode.Walking)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await _buildPlanGraph.ExecuteAsync(
            ownerId,
            planId,
            travelMode,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(PlanGraphMapper.ToDto(result));
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        var userIdClaim =
            User.FindFirstValue("errandflow_user_id");

        return Guid.TryParse(userIdClaim, out ownerId)
            && ownerId != Guid.Empty;
    }
}