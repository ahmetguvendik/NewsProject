using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace IdentityService.Application.Behaviors;

public class CommandLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<CommandLoggingBehavior<TRequest, TResponse>> _logger;

    public CommandLoggingBehavior(ILogger<CommandLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;

        if (!name.EndsWith("Command", StringComparison.Ordinal))
            return await next();

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        using (LogContext.Push(new TargetFieldsEnricher(request, response, name)))
        {
            _logger.LogInformation("{CommandName} tamamlandı ({ElapsedMs} ms).",
                name, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }

    private sealed class TargetFieldsEnricher : ILogEventEnricher
    {
        private readonly object?[] _sources;

        private readonly string _entity;

        public TargetFieldsEnricher(object? request, object? response, string commandName)
        {
            _sources = [request, response];
            _entity = EntityFromCommandName(commandName);
        }

        private static string EntityFromCommandName(string commandName)
        {
            var name = commandName.EndsWith("Command", StringComparison.Ordinal)
                ? commandName[..^"Command".Length]
                : commandName;

            foreach (var verb in (string[])["Create", "Update", "Delete", "Assign", "Remove",
                                            "Activate", "Deactivate", "Publish", "Register"])
            {
                if (name.StartsWith(verb, StringComparison.Ordinal))
                    return name[verb.Length..].ToLowerInvariant();
            }

            return name.ToLowerInvariant();
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            foreach (var source in _sources)
            {
                if (source is null || source.GetType().IsPrimitive)
                    continue;

                foreach (var property in source.GetType().GetProperties())
                {
                    var fieldName = MapName(property.Name);
                    if (fieldName is null)
                        continue;

                    var value = property.GetValue(source);
                    if (value is null)
                        continue;

                    // Komut önce geliyor: aynı alan için komuttaki değer kazanır.
                    logEvent.AddPropertyIfAbsent(factory.CreateProperty(fieldName, value));
                }
            }
        }

        private string? MapName(string propertyName) => propertyName switch
        {
            // Alan adı varlığı zaten söylüyor (UserId, RoleName) → doğrudan kullanılır.
            "RoleName" => "target.role.name",

            "Name" => $"target.{_entity}.name",
            "Title" => $"target.{_entity}.title",

            _ when propertyName.EndsWith("Id", StringComparison.Ordinal) =>
                propertyName.Length == 2
                    ? $"target.{_entity}.id"
                    : $"target.{propertyName[..^2].ToLowerInvariant()}.id",

            _ => null
        };
    }
}
