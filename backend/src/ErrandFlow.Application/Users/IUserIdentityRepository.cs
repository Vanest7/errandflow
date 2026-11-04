
namespace ErrandFlow.Application.Users;

public interface IUserIdentityRepository
{
    Task<Guid?> FindLocalUserIdAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);
}