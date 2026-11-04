using ErrandFlow.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace ErrandFlow.Infrastructure.Persistence.Repositories;

public sealed class UserIdentityRepository : IUserIdentityRepository
{
    private readonly ErrandFlowDbContext _dbContext;

    public UserIdentityRepository(ErrandFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Guid?> FindLocalUserIdAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.IdentityUserId == identityUserId)
            .Select(user => (Guid?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }
}