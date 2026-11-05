using System.Threading.RateLimiting;
using ErrandFlow.Application.Places;
using ErrandFlow.Application.Places.UseCases;
using ErrandFlow.Infrastructure.Places;
using Microsoft.AspNetCore.RateLimiting;
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
using ErrandFlow.Application.Errands.UseCases;
using ErrandFlow.Application.Locations.UseCases;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ApiProblemDetails.InvalidModelState;
    });
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        ApiProblemDetails.Enrich(context.ProblemDetails, context.HttpContext);
});
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
builder.Services.AddScoped<SetPlanLocations>();
builder.Services.AddScoped<RegisterCurrentUser>();
builder.Services.AddScoped<CreateErrand>();
builder.Services.AddScoped<GetErrand>();
builder.Services.AddScoped<UpdateErrand>();
builder.Services.AddScoped<DeleteErrand>();
builder.Services.AddScoped<GetPlanErrands>();
builder.Services.AddScoped<SetErrandSchedule>();
builder.Services.AddScoped<CreateLocation>();
builder.Services.AddScoped<GetLocation>();
builder.Services.AddScoped<GetPlanLocations>();

builder.Services.AddScoped<SearchPlaces>();
builder.Services.AddSingleton<PhotonRequestGate>();
var photonBaseUrl = builder.Configuration["Photon:BaseUrl"] ?? "https://photon.komoot.io/";
if (!Uri.TryCreate(photonBaseUrl, UriKind.Absolute, out var photonUri) ||
    photonUri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(photonUri.Query) ||
    !string.IsNullOrEmpty(photonUri.Fragment) || !string.IsNullOrEmpty(photonUri.UserInfo))
{
    throw new InvalidOperationException("Photon:BaseUrl must be an absolute HTTPS URL.");
}
builder.Services.AddHttpClient<IPlaceSearchProvider, PhotonPlaceSearchProvider>(client =>
{
    client.BaseAddress = new Uri(photonUri.AbsoluteUri.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.MaxResponseContentBufferSize = 1024 * 1024;
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ErrandFlow/1.0");
}).RemoveAllLoggers();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("PlaceSearch", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("errandflow_user_id")?.Value ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

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
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
