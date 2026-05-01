using System.Text.Json;

namespace SteamStorefront.Middleware;

/// <summary>
/// Global exception handler. Wraps every request in a try/catch so unhandled exceptions
/// are caught in one place rather than leaking ASP.NET Core's default HTML error page or
/// an unformatted 500 response. Returns a consistent JSON error body for all failures.
/// </summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Invokes the next middleware in the pipeline. On any unhandled exception, logs the error
    /// and writes a standardized JSON error response instead of propagating the exception.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, ex);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            status = 500,
            error = "An unexpected error occurred."
        }, JsonOptions);

        await context.Response.WriteAsync(body);
    }
}
