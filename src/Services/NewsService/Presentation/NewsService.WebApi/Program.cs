using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application;
using NewsService.Persistance;
using NewsService.WebApi.Infrastructure;
using Shared.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

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
    });

builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

builder.Services.AddApplicationServices();
builder.Services.AddPersistanceServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
