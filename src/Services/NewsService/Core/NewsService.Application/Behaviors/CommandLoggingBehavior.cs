using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace NewsService.Application.Behaviors;

/// <summary>
/// Her komutun çalıştığını ve ne kadar sürdüğünü kaydeder.
///
/// Handler'lara tek tek logger enjekte etmek yerine buraya konuldu: 19 handler'ın
/// hepsini kapsıyor ve yirmincisi yazıldığında kendiliğinden kapsayacak. Aynı
/// gerekçeyle ValidationBehavior ve CachingBehavior da pipeline'da.
///
/// SORGULAR KAPSAM DIŞI. Okuma uçları saniyede defalarca çağrılıyor; hepsini
/// loglamak, bu iş asıl çözmeye çalıştığımız gürültü sorununu geri getirirdi.
/// Ayrım isim sonekiyle yapılıyor (projede tutarlı: ...Command / ...Query).
///
/// İSTEK İÇERİĞİ LOGLANMIYOR, yalnızca komutun adı. Nesneyi olduğu gibi yazmak
/// pratik görünürdü ama RegisterUserCommand parola taşıyor — genel bir
/// serileştirme onu da log'a düşürürdü.
///
/// Hatalar burada yakalanmıyor: GlobalExceptionHandler zaten detayıyla
/// kaydediyor ve aynı trace altında göründüğü için ikinci bir satıra gerek yok.
/// </summary>
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

        // Kimlik alanları log'a ayrı ayrı yazılıyor: aksi halde eylemin HEDEFİ
        // yalnızca url.path içine gömülü kalıyor ve aranamıyor. "Kim kimi pasife
        // aldı" sorusu, aktörü ekleyen UserEnricher ile birlikte cevaplanabiliyor.
        using (LogContext.Push(new TargetFieldsEnricher(request, response, name)))
        {
            _logger.LogInformation("{CommandName} tamamlandı ({ElapsedMs} ms).",
                name, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }

    /// <summary>
    /// Eylemin hedefini <c>target.*</c> öneki altında log'a ekler.
    ///
    /// İSİMLENDİRME BİLİNÇLİ:
    ///   actor.id         → eylemi YAPAN   (UserEnricher, token'dan)
    ///   target.user.id   → eylemin HEDEFİ (burada, komut ve yanıttan)
    ///
    /// HEM KOMUT HEM YANIT OKUNUYOR. Yalnızca komut okunduğunda oluşturma
    /// işlemleri kimliksiz kalıyordu: CreateCategoryCommand'da yeni kategorinin
    /// Id'si henüz yok, handler çalıştıktan sonra oluşuyor. Log "bir kategori
    /// eklendi" diyor ama hangisi olduğunu söylemiyordu.
    ///
    /// GÜVENLİK — beyaz liste, kara liste değil:
    ///   • adı "Id" ile biten her alan (UserId, ArticleId, ...)
    ///   • TAM eşleşen birkaç adlandırma alanı: Name, Title, RoleName
    ///
    /// "Name" bilerek TAM eşleşme: FirstName ve LastName böylece dışarıda kalıyor.
    /// RegisterUserCommand ayrıca Email ve Password taşıyor — beyaz liste bunları
    /// da, yarın eklenecek hassas bir alanı da kendiliğinden dışarıda tutuyor.
    /// </summary>
    private sealed class TargetFieldsEnricher : ILogEventEnricher
    {
        private readonly object?[] _sources;

        private readonly string _entity;

        public TargetFieldsEnricher(object? request, object? response, string commandName)
        {
            _sources = [request, response];
            _entity = EntityFromCommandName(commandName);
        }

        /// <summary>
        /// Komut adından varlık türünü çıkarır: DeleteArticleCommand → "article".
        ///
        /// Gerekli çünkü bazı komutlarda alan adı yalın: DeleteArticleCommand'ın
        /// tek alanı "Id". Tür bilgisi olmadan log'a target.id yazılıyordu ve
        /// karışık bir akışta bunun haber mi kategori mi kullanıcı mı olduğu
        /// anlaşılmıyordu. Tür komut adında zaten var, oradan alınıyor.
        /// </summary>
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

        /// <summary>
        /// UserId → target.user.id, ArticleId → target.article.id, Id → target.id,
        /// Name → target.name. Beyaz listede olmayan her şey için null döner.
        /// </summary>
        private string? MapName(string propertyName) => propertyName switch
        {
            // Alan adı varlığı zaten söylüyor (UserId, RoleName) → doğrudan kullanılır.
            "RoleName" => "target.role.name",

            // Yalın adlar: tür komut adından tamamlanıyor.
            //   DeleteArticleCommand.Id   → target.article.id
            //   CreateCategoryCommand.Name → target.category.name
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
