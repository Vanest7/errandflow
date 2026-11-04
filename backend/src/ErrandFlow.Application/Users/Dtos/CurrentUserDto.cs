namespace ErrandFlow.Application.Users;

public sealed record CurrentUserDto(
    Guid Id,
    string DisplayName,
    string PreferredLanguage
);