using System.Net;
using System.Text.Json;
using Domain.Exceptions;

namespace identiverse_backend.Middleware;

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
        catch (IdentiverseException ex)
        {
            _logger.LogWarning(ex, "Identiverse exception occurred. Status: {StatusCode}, TraceId: {TraceId}", 
                ex.StatusCode, context.TraceIdentifier);
            
            await WriteProblemAsync(
                context,
                statusCode: ex.StatusCode,
                title: ex.Title ?? "Identiverse error",
                type: ex.Type ?? "https://example.com/errors/identiverse",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. Status: 500, TraceId: {TraceId}", 
                context.TraceIdentifier);
            
            await WriteProblemAsync(context, statusCode: (int)HttpStatusCode.InternalServerError,
                title: "Internal Server Error",
                type: "https://errors.identiverse.dev/internal-server-error",
                detail: "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string type,
        string detail)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
        }

        var payload = new
        {
            type,
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier
        };
        
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
        
        await context.Response.WriteAsync(json);
    }
}