# Grafana'da uyarı YOK

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

## Neden burada bir dosya var

Bir süre bu klasör gerçekten boştu ve üstteki açıklama yalnızca bir nottu. Sonra
ortaya çıktı ki yetmiyor: uyarı kuralları Grafana'nın kendi veritabanında, kalıcı
bir volume'da duruyor. Bir kez oluşturulmuş kural, provisioning dosyası silinse de
orada yaşamaya devam ediyor.

19 Eylül'deki testte durum buydu — bu dosya "uyarılar Grafana'dan çıkmıyor" derken
çalışan Grafana'da "Hata oranı yüksek" kuralı canlı değerlendiriliyor ve kök
bildirim politikası Slack'e bakıyordu. Eşik aşılsaydı sağlık panelinin yanında
ikinci bir Slack mesajı daha çıkacaktı.

`uyarilari-kaldir.yml` bu yüzden var: niyeti yazıdan kurala çeviriyor. Grafana her
açılışta kuralı, Slack kanalını ve kök politikayı temizliyor. Yeni bir kurulumda
silinecek bir şey bulamaz, sessizce geçer.
