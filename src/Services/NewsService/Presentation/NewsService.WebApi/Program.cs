using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NewsService.Application;
using NewsService.Persistance;
using NewsService.WebApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
