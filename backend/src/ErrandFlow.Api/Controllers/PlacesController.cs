using ErrandFlow.Application.Places.Dtos;
using ErrandFlow.Application.Places.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ErrandFlow.Api.Controllers;

[ApiController]
[Route("api/places")]
[Authorize(Policy = "RegisteredUser")]
[EnableRateLimiting("PlaceSearch")]
public sealed class PlacesController : ControllerBase
{
    private readonly SearchPlaces _searchPlaces;

    public PlacesController(SearchPlaces searchPlaces)
    {
        _searchPlaces = searchPlaces;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<PlaceSearchResultDto>>> SearchAsync(
        [FromQuery] PlaceSearchInput input,
        CancellationToken cancellationToken)
    {
        var results = await _searchPlaces.ExecuteAsync(input, cancellationToken);
        return Ok(results);
    }
}
