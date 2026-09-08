using Microsoft.AspNetCore.Diagnostics;
using Shared.Exceptions;
using Shared.Models;
using System.Text.Json;

namespace NotificationService.WebApi.Infrastructure;

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

        // Seviye ve yığın izi, hatanın TÜRÜNE göre ayrılıyor.
        //
        // Beklenmeyen hata (5xx) gerçek bir arıza: yığın izi olmadan teşhis edilemez.
        //
        // İş kuralı ihlali (4xx) ise normal bir sonuç — "bu haber zaten yayında"
        // demek sistemde bir şey bozuk demek değil. Exception NESNESİ bilerek
        // geçilmiyor: geçilseydi her rutin 409 için belgenin yarısını kaplayan bir
        // yığın izi yazılır, gerçek hatalar bu gürültünün altında kalırdı.
        //
        // Yetkilendirme hataları ayrı tutuluyor: tek tek normal olsalar da
        // tekrarlandıklarında saldırı işareti olabilirler.
        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Beklenmeyen hata. TraceId={TraceId}", httpContext.TraceIdentifier);
        }
        else if (statusCode is 401 or 403)
        {
            _logger.LogWarning("Yetkilendirme reddi [{ErrorCode}]: {ErrorMessage}. Status={StatusCode}",
                response.ErrorCode, exception.Message, statusCode);
        }
        else
        {
            _logger.LogInformation("İş kuralı ihlali [{ErrorCode}]: {ErrorMessage}. Status={StatusCode}",
                response.ErrorCode, exception.Message, statusCode);
        }

        if (_env.IsDevelopment() && statusCode >= 500)
        {
            response = response with { Description = exception.ToString() };
        }

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, _jsonOptions),
            cancellationToken);

        return true;
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
                Errors      = app is ValidationException ve ? ve.Errors : null
            };
            return (app.StatusCode, response);
        }

        var fallback = new ErrorResponse
        {
            Status      = 500,
            ErrorCode   = ErrorCodes.General.InternalError,
            Message     = "Sunucu taraflı bir hata oluştu.",
            Description = "Beklenmeyen bir hata meydana geldi. Lütfen daha sonra tekrar deneyin."
        };
        return (500, fallback);
    }
}
