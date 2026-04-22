using Microsoft.AspNetCore.Diagnostics;
using Shared.Exceptions;
using Shared.Models;
using System.Text.Json;

namespace IdentityService.WebApi.Infrastructure;

/// <summary>
/// Tüm işlenmeyen exception'ları yakalar ve standart <see cref="ErrorResponse"/>
/// formatında JSON döner. Geliştiriciye yönelik stack trace yalnızca Development
/// ortamında eklenir.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env    = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, response) = BuildResponse(exception);

        // Beklenmeyen hatalar ERROR seviyesinde loglanır; domain hataları WARNING yeterli.
        if (statusCode >= 500)
            _logger.LogError(exception, "Beklenmeyen hata. TraceId={TraceId}", httpContext.TraceIdentifier);
        else
            _logger.LogWarning(exception, "Domain hatası [{Code}]. TraceId={TraceId}",
                response.ErrorCode, httpContext.TraceIdentifier);

        // Development'ta stack trace description'a eklenir.
        if (_env.IsDevelopment() && statusCode >= 500)
        {
            response = response with { Description = exception.ToString() };
        }

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, _jsonOptions),
            cancellationToken);

        return true; // pipeline durduruluyor
    }

    private static (int statusCode, ErrorResponse response) BuildResponse(Exception exception)
    {
        if (exception is AppException app)
        {
            var response = new ErrorResponse
            {
                Status      = app.StatusCode,
                ErrorCode   = app.ErrorCode,
                Message     = app.Message,
                Description = app.Description,
                Errors      = app is Shared.Exceptions.ValidationException ve ? ve.Errors : null
            };
            return (app.StatusCode, response);
        }

        // Beklenmeyen hatalar → 500
        var fallback = new ErrorResponse
        {
            Status      = 500,
            ErrorCode   = Shared.Exceptions.ErrorCodes.General.InternalError,
            Message     = "Sunucu taraflı bir hata oluştu.",
            Description = "Beklenmeyen bir hata meydana geldi. Lütfen daha sonra tekrar deneyin."
        };
        return (500, fallback);
    }
}
