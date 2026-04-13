using MyApp.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace MyApp.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            NotFoundException nfe => (HttpStatusCode.NotFound, nfe.Message),
            UnauthorizedException ue => (HttpStatusCode.Unauthorized, ue.Message),
            ConflictException ce => (HttpStatusCode.Conflict, ce.Message),
            Application.Common.Exceptions.ValidationException ve
                => (HttpStatusCode.BadRequest, string.Join("; ", ve.Errors)),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            error = message
        });

        await context.Response.WriteAsync(response);
    }
}
