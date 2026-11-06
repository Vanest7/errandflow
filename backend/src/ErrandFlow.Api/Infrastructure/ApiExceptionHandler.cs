using ErrandFlow.Application.Places.Exceptions;
using ErrandFlow.Application.Planning.Exceptions;
using ErrandFlow.Application.Plans.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ErrandFlow.Api.Infrastructure;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<ApiExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem;
        if (exception is PlaceSearchUnavailableException)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Place search is temporarily unavailable."
            };
            problem.Extensions["code"] = "place_search_unavailable";
        }
        else if (exception is TravelGraphUnavailableException)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Travel graph is temporarily unavailable."
            };

            problem.Extensions["code"] = "travel_graph_unavailable";
        }
        else if (exception is PlanConcurrencyException)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The plan was changed by another request."
            };
            problem.Extensions["code"] = "plan_revision_conflict";
        }
        else if (exception is RoutePathUnavailableException)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The route path could not be retrieved."
            };

            problem.Extensions["code"] = "route_path_unavailable";
        }
        else if (exception is ArgumentException argument)
        {
            var field = string.IsNullOrEmpty(argument.ParamName)
                ? "request"
                : argument.ParamName;
            problem = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [field] = ["The supplied value is invalid."]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            };
            problem.Extensions["code"] = "validation_failed";
        }
        else
        {
            return false;
        }

        _logger.LogWarning("Request failed with code {ErrorCode}.", problem.Extensions["code"]);
        httpContext.Response.StatusCode = problem.Status!.Value;
        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
        return true;
    }
    
}
