using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace IdentityService.Application.Behaviors;

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
        using (LogContext.Push(new TargetFieldsEnricher(request)))
        {
            _logger.LogInformation("{CommandName} tamamlandı ({ElapsedMs} ms).",
                name, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }

    /// <summary>
    /// Komuttaki kimlik alanlarını <c>target.*</c> öneki altında log'a ekler.
    ///
    /// İSİMLENDİRME BİLİNÇLİ:
    ///   user.id          → eylemi YAPAN   (UserEnricher, token'dan)
    ///   target.user.id   → eylemin HEDEFİ (buradan, komuttan)
    ///
    /// Önce hedef alanları komuttaki adlarıyla (UserId, ArticleId) yazılıyordu ve
    /// log'da "user.id" ile "UserId" yan yana düşüyordu — hangisinin yapan hangisinin
    /// yapılan olduğu okunamıyordu. target. öneki bu belirsizliği kaldırıyor.
    ///
    /// user.* aktör için bırakıldı çünkü ECS'in tanımı bu ve Elastic APM eklendiğinde
    /// aynı alanı o da dolduracak; kendi adımızı uydursaydık ikisi ayrışırdı.
    ///
    /// GÜVENLİK: yalnızca adı "Id" ile biten özellikler ve açıkça izin verilen birkaç
    /// ad okunuyor. İsteği olduğu gibi serileştirmek pratik görünürdü ama
    /// RegisterUserCommand parola taşıyor — beyaz liste, yarın eklenecek hassas bir
    /// alanın kazara log'a düşmesini de engelliyor.
    /// </summary>
    private sealed class TargetFieldsEnricher : ILogEventEnricher
    {
        private readonly object _request;

        public TargetFieldsEnricher(object request) => _request = request;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            foreach (var property in _request.GetType().GetProperties())
            {
                var fieldName = MapName(property.Name);
                if (fieldName is null)
                    continue;

                var value = property.GetValue(_request);
                if (value is null)
                    continue;

                logEvent.AddPropertyIfAbsent(factory.CreateProperty(fieldName, value));
            }
        }

        /// <summary>
        /// UserId → target.user.id, ArticleId → target.article.id, Id → target.id.
        /// Beyaz listede olmayan her şey için null döner, yani log'a yazılmaz.
        /// </summary>
        private static string? MapName(string propertyName)
        {
            if (propertyName == "RoleName")
                return "target.role.name";

            if (!propertyName.EndsWith("Id", StringComparison.Ordinal))
                return null;

            var prefix = propertyName[..^2];

            return prefix.Length == 0
                ? "target.id"
                : $"target.{prefix.ToLowerInvariant()}.id";
        }
    }
}
