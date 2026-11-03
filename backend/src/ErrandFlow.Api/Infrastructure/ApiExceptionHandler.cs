using ErrandFlow.Application.Plans.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        (int statusCode, string title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request."),
            PlanConcurrencyException => (StatusCodes.Status409Conflict, "Plan was modified."),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Plan was modified."),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return false;
        }

        _logger.LogWarning(
            exception,
            "Request failed with status code {StatusCode}.",
            statusCode);

        httpContext.Response.StatusCode = statusCode;

        await _problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title
                }
            });

        return true;
    }
}
