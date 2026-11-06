using ErrandFlow.Infrastructure.Persistence;
using ErrandFlow.Application.Locations;
using ErrandFlow.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using ErrandFlow.Application.Planning;

namespace ErrandFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string password)
    {
        var connectionBuilder = new MySqlConnectionStringBuilder(
            connectionString)
        {
            Password = password
        };
        if (string.IsNullOrWhiteSpace(connectionBuilder.Database))
        {
            throw new InvalidOperationException(
                "The connection string must specify Database=errandflow.");
        }

        if (string.IsNullOrWhiteSpace(connectionBuilder.UserID))
        {
            throw new InvalidOperationException(
                "The database user is missing.");
        }

        if (string.IsNullOrEmpty(connectionBuilder.Password))
        {
            throw new InvalidOperationException(
                "The database password is missing.");
        }
        services.AddDbContext<ErrandFlowDbContext>(options =>
            options.UseMySql(
                connectionBuilder.ConnectionString,
                new MariaDbServerVersion(new Version(11, 8, 6))));

        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IPlanRoutingRepository, PlanRoutingRepository>();
        return services;
    }
}