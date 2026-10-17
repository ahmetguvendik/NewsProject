using Logging.Registration;
using IdentityService.Application;
using IdentityService.Infrastructure;
using IdentityService.Persistence;
using IdentityService.Persistence.Contexts;
using IdentityService.WebApi.BackgroundServices;
using IdentityService.WebApi.Infrastructure;
using IdentityService.WebApi.Middlewares;
using Shared.Extensions;
using HealthCheck.Registration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;
using Shared.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.UseAppLogging("identity-service");

builder.UseAppApm();

// Keycloak'ın parola olaylarını kendi log hattımıza taşır. Parola sıfırlama
// baştan sona Keycloak'ın içinde geçtiği için servislerimiz onu hiç görmüyordu.
builder.Services.AddHostedService<KeycloakEventPollerService>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // [ApiController]'ın varsayılan ValidationProblemDetails formatını devre dışı bırak,
        // tüm servislerle tutarlı ErrorResponse formatına dönüştür.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var response = new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                ErrorCode = ErrorCodes.General.ValidationFailed,
                Message = "Gönderilen veriler geçersiz.",
                Description = "Lütfen hatalı alanları düzelterek tekrar deneyin.",
                Errors = errors
            };

            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<IdentityService.WebApi.Infrastructure.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Docker içi metadata fetch → keycloak:8080
        options.MetadataAddress = $"{builder.Configuration["Keycloak:Authority"]}/.well-known/openid-configuration";
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            // Token localhost'tan alınıyor ama servis Docker içinde — her ikisini de kabul et
            ValidIssuers =
            [
                builder.Configuration["Keycloak:Authority"],
                builder.Configuration["Keycloak:PublicAuthority"]
            ],
            ValidateAudience = false
        };

        // 401/403 yanıtları da diğer hatalarla aynı ErrorResponse gövdesini taşısın.
        options.Events = IdentityService.WebApi.Infrastructure.JwtErrorResponses.Create();
    });

builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// Kafka eklenmedi: WebApi Kafka'ya hiç bağlanmıyor, olayları outbox tablosuna yazıyor.
builder.Services.AddAppHealthChecks(builder.Configuration)
    .AddPostgres()
    .AddRedis()
    .AddKeycloak();

var app = builder.Build();

// Şema, container açılışında migration'lardan oluşturulur (Postgres hazır olana kadar retry'lı).
await app.MigrateDatabaseAsync<IdentityServiceDbContext>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// UseAuthorization'dan sonra: context.User dolu olmalı.
app.UseMiddleware<TokenRevocationMiddleware>();
app.MapControllers();
app.MapAppHealthChecks();

app.Run();
