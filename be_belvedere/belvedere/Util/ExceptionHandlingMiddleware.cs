using System.Text.Json;

namespace belvedere.Util;

/// <summary>
/// Middleware for handling unhandled exceptions and returning consistent error responses.
/// </summary>
/// <remarks>
/// This middleware catches all unhandled exceptions, logs them with full context, and returns
/// a consistent JSON error response to the client. This is crucial for BFF implementations
/// where the frontend relies on predictable error format and status codes.
/// </remarks>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// Invokes the middleware to process the HTTP request and handle any exceptions.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred while processing the request to {Path}",
                            context.Request.Path);
            
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            
            // Return consistent JSON error response
            var errorResponse = new
            {
                type = "https://httpstatuses.com/500",
                title = "Internal Server Error",
                status = StatusCodes.Status500InternalServerError,
                detail = "An unexpected error occurred while processing your request. Please try again later.",
                traceId = context.TraceIdentifier
            };
            
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}
