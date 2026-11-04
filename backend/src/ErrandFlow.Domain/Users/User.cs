

namespace ErrandFlow.Domain.Users;

public sealed class User
{
     public Guid Id { get; private set; }

    public string? IdentityUserId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string PreferredLanguage { get; private set; } = "es";

    public DateTime CreatedAt { get; private set; }

    private User()
    {
    }
}
