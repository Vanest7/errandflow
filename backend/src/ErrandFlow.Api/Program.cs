using ErrandFlow.Infrastructure;
using ErrandFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ErrandFlow.Application.Plans;
using ErrandFlow.Infrastructure.Persistence.Repositories;
using ErrandFlow.Application.Plans.UseCases;
using ErrandFlow.Api.Infrastructure;
using ErrandFlow.Api.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ErrandFlow.Application.Users;
using ErrandFlow.Application.Users.UseCases;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authority = builder.Configuration["Authentication:Jwt:Authority"]
            ?? throw new InvalidOperationException(
                "Authentication:Jwt:Authority is missing.");
        var audience = builder.Configuration["Authentication:Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Authentication:Jwt:Audience is missing.");

        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };

        options.MapInboundClaims = false;

    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RegisteredUser", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new RegisteredUserRequirement());
    });
});
builder.Services.AddScoped<IAuthorizationHandler, RegisteredUserAuthorizationHandler>();
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
builder.Services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
builder.Services.AddScoped<CreatePlan>();
builder.Services.AddScoped<GetMyPlans>();
builder.Services.AddScoped<GetPlan>();
builder.Services.AddScoped<UpdatePlan>();
builder.Services.AddScoped<DeletePlan>();
builder.Services.AddScoped<RegisterCurrentUser>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevelopment", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors("AngularDevelopment");
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
