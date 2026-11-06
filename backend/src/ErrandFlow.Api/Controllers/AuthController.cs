using System.Security.Claims;
using ErrandFlow.Application.Users;
using ErrandFlow.Application.Users.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErrandFlow.Api.Controllers;

public sealed record RegisterCurrentUserRequest(string? DisplayName);

public sealed record AuthMeResponse(
    string ExternalUserId,
    Guid? LocalUserId,
    bool IsRegistered);

[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterCurrentUser _registerCurrentUser;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public AuthController(
        RegisterCurrentUser registerCurrentUser,
        IUserIdentityRepository userIdentityRepository)
    {
        _registerCurrentUser = registerCurrentUser;
        _userIdentityRepository = userIdentityRepository;
    }

    [HttpGet("me")]
    public async Task<ActionResult<AuthMeResponse>> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var externalUserId = User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(externalUserId))
        {
            return Unauthorized();
        }

        var localUserId = await _userIdentityRepository.FindLocalUserIdAsync(
            externalUserId,
            cancellationToken);

        return Ok(new AuthMeResponse(
            externalUserId,
            localUserId,
            localUserId.HasValue));
    }

    [HttpPost("me")]
    public async Task<ActionResult<CurrentUserDto>> RegisterAsync(
        [FromBody] RegisterCurrentUserRequest request,
        CancellationToken cancellationToken)
    {
        var externalUserId = User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(externalUserId))
        {
            return Unauthorized();
        }

        var user = await _registerCurrentUser.ExecuteAsync(
            externalUserId,
            request.DisplayName,
            cancellationToken);

        return Ok(user);
    }
}