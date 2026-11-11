using System.Net;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            var statusCode = ex switch
            {
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                ValidationException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.Conflict,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            var isUnexpected = statusCode == (int)HttpStatusCode.InternalServerError;

            var payload = new
            {
                title = isUnexpected ? "An unexpected error occurred." : "Request failed.",
                status = statusCode,
                detail = isUnexpected ? "The request could not be completed." : ex.Message,
                error = isUnexpected ? "The request could not be completed." : ex.Message,
                type = $"https://httpstatuses.com/{statusCode}"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
