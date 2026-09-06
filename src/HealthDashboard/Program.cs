// Üç servisin sağlık durumunu tek sayfada toplayan panel.
//
// Neden ayrı bir container: panel, izlediği servislerden birinin içinde koşsaydı
// o servis çöktüğünde panel de çökerdi — yani tam ihtiyaç duyulduğu anda erişilemez
// olurdu. Bağımsız çalışıp servisleri dışarıdan yokluyor.

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHealthChecksUI(settings =>
    {
        settings.SetEvaluationTimeInSeconds(
            builder.Configuration.GetValue("HealthChecksUI:EvaluationSeconds", 15));

        settings.SetApiMaxActiveRequests(1);

        // İzlenen uçlar appsettings/compose'dan geliyor (HealthChecksUI:HealthChecks).
        // Kod yerine ayarda tutuluyor ki yeni bir servis eklendiğinde panel
        // yeniden derlenmesin.
    })
    // Depolama in-memory: panel yeniden başladığında geçmiş sıfırlanır. Anlık
    // durumu görmek için yeterli; kalıcı geçmiş isteniyorsa Postgres sağlayıcısına
    // geçilebilir, o zaman ayrı bir veritabanı gerekir.
    .AddInMemoryStorage();

var app = builder.Build();

// Panelin kendisi kök adreste açılsın; ayrıca /health-ui altındaki API'yi kullanır.
app.MapHealthChecksUI(options => options.UIPath = "/");

app.Run();
