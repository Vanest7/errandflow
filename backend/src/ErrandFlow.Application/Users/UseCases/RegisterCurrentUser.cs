using ErrandFlow.Domain.Users;

namespace ErrandFlow.Application.Users.UseCases;

public sealed class RegisterCurrentUser
{
    private readonly IUserIdentityRepository _userRepository;

    public RegisterCurrentUser(IUserIdentityRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<CurrentUserDto> ExecuteAsync(
        string identityUserId,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            throw new ArgumentException(
                "External identity is required.",
                nameof(identityUserId));
        }

        var existingUser = await _userRepository.FindByIdentityAsync(
            identityUserId,
            cancellationToken);

        if (existingUser is not null)
        {
            return ToDto(existingUser);
        }

        var user = new User(
            identityUserId,
            string.IsNullOrWhiteSpace(displayName)
                ? "ErrandFlow user"
                : displayName);

        var savedUser = await _userRepository.CreateIfMissingAsync(
            user,
            cancellationToken);

        return ToDto(savedUser);
    }

    private static CurrentUserDto ToDto(User user)
    {
        return new CurrentUserDto(
            user.Id,
            user.DisplayName,
            user.PreferredLanguage);
    }
}