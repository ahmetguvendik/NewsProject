/**
 * Adresten gelen bir kimliğin GUID biçiminde olup olmadığını söyler.
 *
 * Route tanımları (`haber/:id`, `haber/:id/duzenle`) :id yerine ne gelirse
 * kabul ediyor. Biçimi bozuk bir kimliği sunucuya sormanın karşılığı yok:
 * ASP.NET routing bunu controller'a hiç ulaştırmadan reddediyor ve cevap
 * GÖVDESİZ geliyor. Gövde olmayınca istemci de anlamlı bir mesaj çıkaramayıp
 * "İstek başarısız oldu (HTTP 404) · UNKNOWN" yazıyordu.
 *
 * Bu yüzden kontrol istekten ÖNCE yapılıyor: kullanıcı doğru dürüst bir
 * "sayfa bulunamadı" görüyor, boşa giden istek ve onun ürettiği log satırı da
 * hiç oluşmuyor.
 *
 * Geçerli GUID ama sistemde yoksa durum farklı — orada backend düzgün bir
 * ErrorResponse dönüyor ("Makale bulunamadı." + açıklaması) ve o cevabı
 * göstermek istiyoruz.
 */
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function isUuid(value: string | undefined): boolean {
  return value !== undefined && UUID.test(value)
}
