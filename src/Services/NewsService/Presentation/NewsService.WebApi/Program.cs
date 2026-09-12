using Logging.Registration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application;
using NewsService.Persistance;
using NewsService.Persistance.Contexts;
using NewsService.WebApi.Infrastructure;
using Shared.Extensions;
using HealthCheck.Registration;
using Shared.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.UseAppLogging("news-service");

// Elastic APM: HTTP istekleri, EF Core sorguları ve HttpClient çağrıları
// kendiliğinden enstrümante ediliyor.
//
// Loglarla bağlantıyı trace.id kuruyor: loglarımız zaten ECS formatında ve bu
// alanı taşıyor, agent da aynısını yazıyor. Kibana'da bir trace'ten onun
// loglarına geçmek bu sayede doğrudan çalışıyor.
//
// Ayarlar ortam değişkeninden (bkz. docker-compose.yml): sunucu adresi, servis
// adı ve ortam. Servis adı loglardaki service.name ile AYNI olmalı, yoksa iki
// taraf birbirini tanımaz.
builder.Services.AddAllElasticApm();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
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
                ErrorCode = "VALIDATION_ERROR",
                Message = "Gönderilen veriler geçersiz.",
                Description = "Lütfen hatalı alanları düzelterek tekrar deneyin.",
                Errors = errors
            };

            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<NewsService.WebApi.Infrastructure.GlobalExceptionHandler>();
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
        options.Events = JwtErrorResponses.Create();
    });

builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

builder.Services.AddApplicationServices();
builder.Services.AddPersistanceServices(builder.Configuration);

// Kafka eklenmedi: WebApi Kafka'ya hiç bağlanmıyor, olayları outbox tablosuna yazıyor.
// Kafka'yı burada raporlamak olmayan bir bağımlılık varmış izlenimi verirdi.
builder.Services.AddAppHealthChecks(builder.Configuration)
    .AddPostgres()
    .AddRedis()
    .AddKeycloak()
    .AddStorage();

var app = builder.Build();

// Şema, container açılışında migration'lardan oluşturulur (Postgres hazır olana kadar retry'lı).
await app.MigrateDatabaseAsync<NewsServiceDbContext>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapAppHealthChecks();

app.Run();
