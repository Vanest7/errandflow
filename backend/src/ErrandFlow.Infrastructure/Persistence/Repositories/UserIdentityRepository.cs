using ErrandFlow.Application.Users;
using ErrandFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

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

    public Task<User?> FindByIdentityAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.IdentityUserId == identityUserId,
                cancellationToken);
    }

    public async Task<User> CreateIfMissingAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return user;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is MySqlException { Number: 1062 }) //Error number from MariaDB
        {
            _dbContext.Entry(user).State = EntityState.Detached;

            var existingUser = await FindByIdentityAsync(
                user.IdentityUserId!,
                cancellationToken);

            if (existingUser is not null)
            {
                return existingUser;
            }

            throw;
        }
    }
}