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

    public LocationsController(
        CreateLocation createLocation,
        GetLocation getLocation)
    {
        _createLocation = createLocation;
        _getLocation = getLocation;
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

    private bool TryGetOwnerId(out Guid ownerId)
    {
        var userIdClaim = User.FindFirstValue("errandflow_user_id");

        return Guid.TryParse(userIdClaim, out ownerId)
            && ownerId != Guid.Empty;
    }
}