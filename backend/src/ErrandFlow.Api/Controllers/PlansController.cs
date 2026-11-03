using System.Security.Claims;
using ErrandFlow.Application.Plans.Dtos;
using ErrandFlow.Application.Plans.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErrandFlow.Api.Controllers;

[ApiController]
[Route("api/plans")]
[Authorize]
public sealed class PlansController : ControllerBase
{
    private readonly CreatePlan _createPlan;
    private readonly GetMyPlans _getMyPlans;
    private readonly GetPlan _getPlan;
    private readonly UpdatePlan _updatePlan;
    private readonly DeletePlan _deletePlan;

    public PlansController(
        CreatePlan createPlan,
        GetMyPlans getMyPlans,
        GetPlan getPlan,
        UpdatePlan updatePlan,
        DeletePlan deletePlan)
    {
        _createPlan = createPlan;
        _getMyPlans = getMyPlans;
        _getPlan = getPlan;
        _updatePlan = updatePlan;
        _deletePlan = deletePlan;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlanSummaryDto>>> GetMyPlansAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var plans = await _getMyPlans.ExecuteAsync(ownerId, cancellationToken);
        return Ok(plans);
    }

    [HttpGet("{planId:guid}")]
    public async Task<ActionResult<PlanDto>> GetPlanAsync(
        Guid planId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var plan = await _getPlan.ExecuteAsync(
            ownerId,
            planId,
            cancellationToken);

        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpPost]
    public async Task<ActionResult<PlanDto>> CreatePlanAsync(
        PlanInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var plan = await _createPlan.ExecuteAsync(
            ownerId,
            input,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetPlanAsync),
            new { planId = plan.Id },
            plan);
    }

    [HttpPut("{planId:guid}")]
    public async Task<ActionResult<PlanDto>> UpdatePlanAsync(
        Guid planId,
        PlanInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var plan = await _updatePlan.ExecuteAsync(
            ownerId,
            planId,
            input,
            cancellationToken);

        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpDelete("{planId:guid}")]
    public async Task<IActionResult> DeletePlanAsync(
        Guid planId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var deleted = await _deletePlan.ExecuteAsync(
            ownerId,
            planId,
            cancellationToken);

        return deleted ? NoContent() : NotFound();
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out ownerId);
    }
}
