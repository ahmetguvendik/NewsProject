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
            
            var expired = context.AuthenticateFailure is SecurityTokenExpiredException;

            return WriteAsync(context.Response, new ErrorResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                ErrorCode = expired ? ErrorCodes.Auth.TokenExpired : ErrorCodes.Auth.Unauthorized,
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
