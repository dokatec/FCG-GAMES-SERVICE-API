using System.Net;
using System.Text.Json;

namespace FCG.API.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log Estruturado: O .NET salva os parâmetros separadamente
            _logger.LogError(ex, "Erro capturado no Middleware: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;

        // Se for erro de validação do domínio, retornamos 400 Bad Request
        if (exception is ArgumentException) code = HttpStatusCode.BadRequest;

        var result = JsonSerializer.Serialize(new
        {
            message = exception.Message,
            type = exception.GetType().Name
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(result);
    }
}