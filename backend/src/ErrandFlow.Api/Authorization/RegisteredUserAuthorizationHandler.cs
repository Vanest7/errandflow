using System.Security.Claims;
using ErrandFlow.Application.Users;
using Microsoft.AspNetCore.Authorization;

namespace ErrandFlow.Api.Authorization;

public sealed class RegisteredUserAuthorizationHandler
    : AuthorizationHandler<RegisteredUserRequirement>
{
    private const string LocalUserClaimType = "errandflow_user_id";
    private readonly IUserIdentityRepository _userRepository;

    public RegisteredUserAuthorizationHandler(
        IUserIdentityRepository userRepository)
    {
        _userRepository = userRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RegisteredUserRequirement requirement)
    {
        var externalUserId = context.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(externalUserId) ||
            externalUserId.Length > 255)
        {
            return;
        }

        var localUserId = await _userRepository.FindLocalUserIdAsync(
            externalUserId,
            (context.Resource as HttpContext)?.RequestAborted
                ?? CancellationToken.None);

        if (!localUserId.HasValue)
        {
            return;
        }

        var identity = context.User.Identity as ClaimsIdentity;

        if (identity is null)
        {
            return;
        }

        foreach (var claim in identity.FindAll(LocalUserClaimType).ToArray())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(
            LocalUserClaimType,
            localUserId.Value.ToString("D")));

        context.Succeed(requirement);
    }
}
