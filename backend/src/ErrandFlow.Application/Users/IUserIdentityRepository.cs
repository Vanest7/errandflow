
using ErrandFlow.Domain.Users;

namespace ErrandFlow.Application.Users;

public interface IUserIdentityRepository
{
    Task<Guid?> FindLocalUserIdAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<User?> FindByIdentityAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<User> CreateIfMissingAsync(
        User user,
        CancellationToken cancellationToken = default);
}