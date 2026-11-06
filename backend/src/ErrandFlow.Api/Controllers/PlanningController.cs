using System.Security.Claims;
using ErrandFlow.Application.Planning;
using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Planning.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErrandFlow.Api.Controllers;

[ApiController]
[Route("api/plans/{planId:guid}/planning")]
[Authorize(Policy = "RegisteredUser")]
public sealed class PlanningController : ControllerBase
{
    private readonly BuildPlanGraph _buildPlanGraph;
    private readonly EvaluateOriginalPlan _evaluateOriginalPlan;
    private readonly OptimizePlan _optimizePlan;
    private readonly BuildOptimizedPlanRoute _buildOptimizedPlanRoute;

    public PlanningController(
        BuildPlanGraph buildPlanGraph,
        EvaluateOriginalPlan evaluateOriginalPlan,
        OptimizePlan optimizePlan,
        BuildOptimizedPlanRoute buildOptimizedPlanRoute)
    {
        _buildPlanGraph = buildPlanGraph;
        _evaluateOriginalPlan = evaluateOriginalPlan;
        _optimizePlan = optimizePlan;
        _buildOptimizedPlanRoute = buildOptimizedPlanRoute;
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

    [HttpGet("original")]
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<PlanScheduleDto>> GetOriginalScheduleAsync(
        Guid planId,
        CancellationToken cancellationToken,
        [FromQuery] TravelMode travelMode = TravelMode.Walking)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await _evaluateOriginalPlan.ExecuteAsync(
            ownerId,
            planId,
            travelMode,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(PlanScheduleMapper.ToDto(result));
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        var userIdClaim =
            User.FindFirstValue("errandflow_user_id");

        return Guid.TryParse(userIdClaim, out ownerId)
            && ownerId != Guid.Empty;
    }
    [HttpGet("optimized")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public async Task<ActionResult<PlanOptimizationDto>> GetOptimizedScheduleAsync(
    Guid planId,
    CancellationToken cancellationToken,
    [FromQuery] TravelMode travelMode = TravelMode.Walking)
{
    if (!TryGetOwnerId(out var ownerId))
    {
        return Unauthorized();
    }

    var result = await _optimizePlan.ExecuteAsync(
        ownerId,
        planId,
        travelMode,
        cancellationToken);

    if (result is null)
    {
        return NotFound();
    }

    return Ok(PlanOptimizationMapper.ToDto(result));
}
[HttpGet("optimized/route")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public async Task<ActionResult<OptimizedPlanRouteDto>>
    GetOptimizedRouteAsync(
        Guid planId,
        CancellationToken cancellationToken,
        [FromQuery] TravelMode travelMode = TravelMode.Walking)
{
    if (!TryGetOwnerId(out var ownerId))
    {
        return Unauthorized();
    }

    var result = await _buildOptimizedPlanRoute.ExecuteAsync(
        ownerId,
        planId,
        travelMode,
        cancellationToken);

    if (result is null)
    {
        return NotFound();
    }

    return Ok(result);
}
}