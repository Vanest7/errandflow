using ErrandFlow.Domain.Plans;
using ErrandFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ErrandFlow.Infrastructure.Persistence;

public sealed class ErrandFlowDbContext : DbContext
{
    public ErrandFlowDbContext(
        DbContextOptions<ErrandFlowDbContext> options)
        : base(options)
    {
    }

      public DbSet<Plan> Plans => Set<Plan>();
      public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ErrandFlowDbContext).Assembly);
    }
}