using System.Security.Claims;
using ErrandFlow.Application.Locations.Dtos;
using ErrandFlow.Application.Locations.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErrandFlow.Api.Controllers;

[ApiController]
[Route("api/plans/{planId:guid}/locations")]
[Authorize(Policy = "RegisteredUser")]
public sealed class LocationsController : ControllerBase
{
    private readonly CreateLocation _createLocation;
    private readonly GetLocation _getLocation;
    private readonly GetPlanLocations _getPlanLocations;
    private readonly UpdateLocation _updateLocation;
    private readonly DeleteLocation _deleteLocation;

    public LocationsController(
        CreateLocation createLocation,
        GetLocation getLocation,
        GetPlanLocations getPlanLocations,
        UpdateLocation updateLocation,
        DeleteLocation deleteLocation)
    {
        _createLocation = createLocation;
        _getLocation = getLocation;
        _getPlanLocations = getPlanLocations;
        _updateLocation = updateLocation;
        _deleteLocation = deleteLocation;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> GetPlanLocationsAsync(
        Guid planId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var locations = await _getPlanLocations.ExecuteAsync(
            ownerId,
            planId,
            cancellationToken);

        return locations is null ? NotFound() : Ok(locations);
    }

    [HttpGet("{locationId:guid}", Name = "GetLocationById")]
    public async Task<ActionResult<LocationDto>> GetAsync(
        Guid planId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var location = await _getLocation.ExecuteAsync(
            ownerId,
            planId,
            locationId,
            cancellationToken);

        return location is null ? NotFound() : Ok(location);
    }

    [HttpPost]
    public async Task<ActionResult<LocationMutationResult>> CreateAsync(
        Guid planId,
        CreateLocationInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await _createLocation.ExecuteAsync(
            ownerId,
            planId,
            input,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return CreatedAtRoute(
            "GetLocationById",
            new
            {
                planId,
                locationId = result.Location.Id
            },
            result);
    }

    [HttpPut("{locationId:guid}")]
    public async Task<ActionResult<LocationMutationResult>> UpdateAsync(
        Guid planId,
        Guid locationId,
        UpdateLocationInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId)) return Unauthorized();

        var result = await _updateLocation.ExecuteAsync(
            ownerId, planId, locationId, input, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{locationId:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid planId,
        Guid locationId,
        [FromQuery] ulong expectedRevision,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId)) return Unauthorized();

        var deleted = await _deleteLocation.ExecuteAsync(
            ownerId, planId, locationId, expectedRevision, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        var userIdClaim = User.FindFirstValue("errandflow_user_id");

        return Guid.TryParse(userIdClaim, out ownerId)
            && ownerId != Guid.Empty;
    }
}