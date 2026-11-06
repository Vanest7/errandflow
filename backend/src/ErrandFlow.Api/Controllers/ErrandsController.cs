using System.Security.Claims;
using ErrandFlow.Application.Errands.Dtos;
using ErrandFlow.Application.Errands.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErrandFlow.Api.Controllers;

[ApiController]
[Route("api/plans/{planId:guid}/errands")]
[Authorize(Policy = "RegisteredUser")]
public sealed class ErrandsController : ControllerBase
{
    private readonly GetPlanErrands _getPlanErrands;
    private readonly GetErrand _getErrand;
    private readonly CreateErrand _createErrand;
    private readonly UpdateErrand _updateErrand;
    private readonly DeleteErrand _deleteErrand;
    private readonly SetErrandSchedule _setErrandSchedule;
    private readonly ReorderErrands _reorderErrands;

    public ErrandsController(
        GetPlanErrands getPlanErrands,
        GetErrand getErrand,
        CreateErrand createErrand,
        UpdateErrand updateErrand,
        DeleteErrand deleteErrand,
        SetErrandSchedule setErrandSchedule,
        ReorderErrands reorderErrands)
    {
        _getPlanErrands = getPlanErrands;
        _getErrand = getErrand;
        _createErrand = createErrand;
        _updateErrand = updateErrand;
        _deleteErrand = deleteErrand;
        _setErrandSchedule = setErrandSchedule;
        _reorderErrands = reorderErrands;
    }

    [HttpPut("{errandId:guid}/schedule")]
    public async Task<ActionResult<ErrandMutationResult>> SetScheduleAsync(
        Guid planId,
        Guid errandId,
        SetErrandScheduleInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await _setErrandSchedule.ExecuteAsync(
            ownerId,
            planId,
            errandId,
            input,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ErrandDto>>> GetAsync(
        Guid planId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var errands = await _getPlanErrands.ExecuteAsync(
            ownerId,
            planId,
            cancellationToken);

        return errands is null ? NotFound() : Ok(errands);
    }

    [HttpPut("order")]
    public async Task<ActionResult<ReorderErrandsResult>> ReorderAsync(
        Guid planId,
        ReorderErrandsInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId)) return Unauthorized();
        var result = await _reorderErrands.ExecuteAsync(
            ownerId, planId, input, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        var userIdClaim = User.FindFirstValue("errandflow_user_id");

        return Guid.TryParse(userIdClaim, out ownerId)
            && ownerId != Guid.Empty;
    }

    [HttpGet("{errandId:guid}", Name = "GetErrandById")]
    public async Task<ActionResult<ErrandDto>> GetByIdAsync(
        Guid planId,
        Guid errandId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var errand = await _getErrand.ExecuteAsync(
            ownerId,
            planId,
            errandId,
            cancellationToken);

        return errand is null ? NotFound() : Ok(errand);
    }

    [HttpPost]
    public async Task<ActionResult<ErrandMutationResult>> CreateAsync(
        Guid planId,
        CreateErrandInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await _createErrand.ExecuteAsync(
            ownerId,
            planId,
            input,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return CreatedAtRoute(
            "GetErrandById",
            new
            {
                planId,
                errandId = result.Errand.Id
            },
            result);
    }

    [HttpPut("{errandId:guid}")]
    public async Task<ActionResult<ErrandMutationResult>> UpdateAsync(
        Guid planId,
        Guid errandId,
        UpdateErrandInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await _updateErrand.ExecuteAsync(
            ownerId,
            planId,
            errandId,
            input,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{errandId:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid planId,
        Guid errandId,
        [FromQuery] ulong? expectedRevision,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var deleted = await _deleteErrand.ExecuteAsync(
            ownerId,
            planId,
            errandId,
            expectedRevision,
            cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}