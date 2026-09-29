using Microsoft.AspNetCore.Diagnostics;
using Shared.Exceptions;
using Shared.Models;
using System.Text.Json;

namespace IdentityService.WebApi.Infrastructure;

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

        // Eş zamanlı iki işlem çakıştı (Serializable transaction). Bu bir arıza
        // değil, korumanın çalışması: yarışı kaybeden işlem iptal edildi, veri
        // tutarlı. Kullanıcıya 500 değil "tekrar deneyin" dönülüyor.
        if (IsConcurrencyConflict(exception))
        {
            var response = new ErrorResponse
            {
                Status      = 409,
                ErrorCode   = Shared.Exceptions.ErrorCodes.General.ConcurrencyConflict,
                Message     = "İşlem başka bir değişiklikle çakıştı.",
                Description = "Aynı kayıt üzerinde eş zamanlı bir işlem yapıldı. Sayfayı yenileyip tekrar deneyin."
            };
            return (409, response);
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

    /// <summary>
    /// Hata zincirinde SQLSTATE 40001 (serialization_failure) ya da 40P01
    /// (deadlock_detected) var mı? İkisi de "eş zamanlı bir işlemle çakıştın,
    /// tekrar dene" demek.
    ///
    /// Zincir yürünüyor çünkü EF hatayı sarmalıyor: SaveChanges sırasında
    /// çıkarsa DbUpdateException'ın içinde, commit sırasında çıkarsa doğrudan
    /// geliyor.
    ///
    /// Npgsql'e referans vermeden: DbException.SqlState standart bir alan ve
    /// kodlar SQL standardından. Sunum katmanı veritabanı sağlayıcısını bilmiyor.
    /// </summary>
    private static bool IsConcurrencyConflict(Exception? exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is System.Data.Common.DbException { SqlState: "40001" or "40P01" })
                return true;
        }

        return false;
    }
}
