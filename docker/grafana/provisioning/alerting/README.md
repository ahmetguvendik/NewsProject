# Burası bilerek boş

Uyarılar Grafana'dan DEĞİL, sağlık kontrollerinden çıkıyor.

Kısa süre Grafana'da bir "hata oranı" kuralı vardı, sonra kaldırıldı. Sebebi:
uyarıların iki ayrı sistemden çıkması, ikisini de doğru yapılandırmayı gerektiriyor
ve hangisinin neyi izlediğini takip etmek zorlaşıyor.

Şimdi tek yol var:

    servis /health ucu  →  HealthCheck.Api paneli  →  Slack webhook

Hata oranı kuralı da oraya taşındı: `ErrorRateHealthCheck`, Elasticsearch'e
"son 5 dakikada kaç Error var" diye soruyor. Eşik compose'da:
`Alerting__ErrorRate__Threshold`.

Grafana bu kurulumda yalnızca PANO için: Elasticsearch veri kaynağı bağlı
(bkz. ../datasources/elasticsearch.yml), grafik ve tablo çizmek için hazır.
