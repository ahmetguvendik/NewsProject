using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Logging.Registration;

public static class ApmExtensions
{
    private static readonly Dictionary<string, string?> Defaults = new()
    {
        // Sağlık yoklamaları kaydedilen transaction'ların %60'ını oluşturuyordu.
        ["ElasticApm:TransactionIgnoreUrls"] = "/health*",

        // Ajanın varsayılan listesi + *cookie*: Cookie başlığı açık metin kaydediliyordu.
        // Liste değiştirilirse varsayılanlar da yazılmalı, yoksa kaybolurlar.
        ["ElasticApm:SanitizeFieldNames"] =
            "password, passwd, pwd, secret, *key, *token*, *session*, *credit*, *card*, *auth*, *cookie*, *principal*"
    };

    public static void UseAppApm(this IHostApplicationBuilder builder)
    {
        // En düşük öncelik: ortam değişkenleri (ElasticApm__...) bu değerleri ezebilir.
        builder.Configuration.Sources.Insert(0, new MemoryConfigurationSource { InitialData = Defaults });

        builder.Services.AddAllElasticApm();
    }
}
