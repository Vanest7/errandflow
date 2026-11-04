using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ErrandFlow.Api.Infrastructure;

internal static class ApiProblemDetails
{
    public static void Enrich(ProblemDetails problem, HttpContext context)
    {
        if (!problem.Extensions.ContainsKey("code"))
        {
            problem.Extensions["code"] = problem.Status switch
            {
                400 => "validation_failed",
                401 => "authentication_required",
                403 => "access_denied",
                404 => "resource_not_found",
                405 => "method_not_allowed",
                415 => "unsupported_media_type",
                >= 500 => "internal_error",
                _ => "request_failed"
            };
        }
        problem.Extensions["traceId"] = context.TraceIdentifier;
    }

    public static IActionResult InvalidModelState(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .GroupBy(entry => string.IsNullOrEmpty(entry.Key)
                ? "request"
                : JsonNamingPolicy.CamelCase.ConvertName(entry.Key))
            .ToDictionary(group => group.Key,
                group => new[] { "The supplied value is missing or invalid." });
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        };
        Enrich(problem, context.HttpContext);
        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
