using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Exceptions;
using Shared.Models;

namespace IdentityService.WebApi.Infrastructure;

public static class JwtErrorResponses
{
    public static JwtBearerEvents Create() => new()
    {
        OnChallenge = context =>
        {
            // Varsayılan gövdesiz yanıtın yazılmasını engelle.
            context.HandleResponse();

            var (code, message, description) = context.AuthenticateFailure switch
            {
                SecurityTokenExpiredException => (ErrorCodes.Auth.TokenExpired, "Oturumunuzun süresi doldu.", "Lütfen tekrar giriş yapın."),
                not null => (ErrorCodes.Auth.TokenInvalid, "Oturum bilgisi geçersiz.", "Lütfen tekrar giriş yapın."),
                null => (ErrorCodes.Auth.Unauthorized, "Bu işlem için giriş yapmalısınız.", "Giriş yaptıktan sonra tekrar deneyin.")
            };

            return WriteAsync(context.Response, new ErrorResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                ErrorCode = code,
                Message = message,
                Description = description
            });
        },

        // Token geçerli ama rol yetmiyor.
        OnForbidden = context => WriteAsync(context.Response, new ErrorResponse
        {
            Status = StatusCodes.Status403Forbidden,
            ErrorCode = ErrorCodes.Auth.Forbidden,
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
