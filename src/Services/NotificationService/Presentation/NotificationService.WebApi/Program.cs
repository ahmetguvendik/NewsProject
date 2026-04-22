using Microsoft.AspNetCore.Mvc;
using NotificationService.Persistance;
using NotificationService.WebApi.Infrastructure;
using Shared.Models;

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
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddPersistanceServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseAuthorization();
app.MapControllers();

app.Run();
