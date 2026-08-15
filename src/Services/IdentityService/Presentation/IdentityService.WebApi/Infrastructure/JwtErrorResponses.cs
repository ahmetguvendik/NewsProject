using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;

namespace IdentityService.WebApi.Infrastructure;

/// <summary>
/// JWT katmanının ürettiği 401/403 yanıtları varsayılan olarak gövdesizdir;
/// client elinde yalnızca status kodu kaldığı için kullanıcıya "İstek başarısız
/// oldu (HTTP 401)" demekten öteye gidemez. Bu olaylar yanıtı diğer tüm
/// hatalarla aynı <see cref="ErrorResponse"/> şekline çevirir.
/// </summary>
public static class JwtErrorResponses
{
    public static JwtBearerEvents Create() => new()
    {
        OnChallenge = context =>
        {
            // Varsayılan gövdesiz yanıtın yazılmasını engelle.
            context.HandleResponse();

            // Süresi dolmuş token ile hiç gönderilmemiş token kullanıcı için
            // farklı durumlar: ilki "tekrar giriş yap", ikincisi "giriş yap".
            var expired = context.AuthenticateFailure is SecurityTokenExpiredException;

            return WriteAsync(context.Response, new ErrorResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                ErrorCode = expired ? "TOKEN_EXPIRED" : "UNAUTHORIZED",
                Message = expired
                    ? "Oturumunuzun süresi doldu."
                    : "Bu işlem için giriş yapmalısınız.",
                Description = expired
                    ? "Lütfen tekrar giriş yapın."
                    : "Giriş yaptıktan sonra tekrar deneyin."
            });
        },

        // Token geçerli ama rol yetmiyor.
        OnForbidden = context => WriteAsync(context.Response, new ErrorResponse
        {
            Status = StatusCodes.Status403Forbidden,
            ErrorCode = "FORBIDDEN",
            Message = "Bu işlem için yetkiniz yok.",
            Description = "Hesabınızın rolü bu işlemi yapmaya izin vermiyor."
        })
    };

    private static Task WriteAsync(HttpResponse response, ErrorResponse body)
    {
        response.StatusCode = body.Status;
        response.ContentType = "application/json; charset=utf-8";
        return response.WriteAsJsonAsync(body);
    }
}
