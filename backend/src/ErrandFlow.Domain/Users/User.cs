

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
     public User(string identityUserId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(identityUserId) ||
            identityUserId.Length > 255)
        {
            throw new ArgumentException(
                "Invalid external identity.",
                nameof(identityUserId));
        }

        if (string.IsNullOrWhiteSpace(displayName) ||
            displayName.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Display name must contain between 1 and 100 characters.",
                nameof(displayName));
        }

        Id = Guid.NewGuid();
        IdentityUserId = identityUserId;
        DisplayName = displayName.Trim();
    }
}
