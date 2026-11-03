using ErrandFlow.Infrastructure;
using ErrandFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ErrandFlow.Application.Plans;
using ErrandFlow.Infrastructure.Persistence.Repositories;
using ErrandFlow.Application.Plans.UseCases;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var connectionString =
    builder.Configuration.GetConnectionString("ErrandFlow")
    ?? throw new InvalidOperationException(
        "The ErrandFlow connection string is missing.");

var databasePassword =
    builder.Configuration["Database:Password"]
    ?? throw new InvalidOperationException(
        "The database password is missing.");

builder.Services.AddInfrastructure(
    connectionString,
    databasePassword);
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<CreatePlan>();
builder.Services.AddScoped<GetMyPlans>();
builder.Services.AddScoped<GetPlan>();
builder.Services.AddScoped<UpdatePlan>();
builder.Services.AddScoped<DeletePlan>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<ErrandFlowDbContext>();

    try
    {
        var plans = await dbContext.Plans
            .AsNoTracking()
            .OrderBy(plan => plan.PlanDate)
            .Take(5)
            .ToListAsync();

        app.Logger.LogInformation(
            "Plans query successful. Records fetched: {Count}",
            plans.Count);
    }
    catch (Exception exception)
    {
        app.Logger.LogError(
            exception,
            "Plans query failed.");
    }
}
{
    await using var scope = app.Services.CreateAsyncScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<ErrandFlowDbContext>();

    var canConnect = await dbContext.Database.CanConnectAsync();

    app.Logger.LogInformation(
        "Database connection successful: {CanConnect}",
        canConnect);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
