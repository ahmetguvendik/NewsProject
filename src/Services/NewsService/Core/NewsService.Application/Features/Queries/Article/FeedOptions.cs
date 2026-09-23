namespace NewsService.Application.Features.Queries.Article;

/// <summary>
/// Herkese açık akışın tazelik penceresi.
///
/// NEDEN VAR: bu bir haber sitesi ve ürün kararı "okuyucu güncel haberi görsün"
/// yönünde. Günde 10 haber girildiğinde bir yılda ~3600 haber birikiyor; liste
/// sayfalamayla erişilebilir kalsa bile 180. sayfanın kimseye faydası yok.
/// Pencere, akışı hep son günlerin haberiyle sınırlıyor.
///
/// NEREYE UYGULANIYOR: yalnızca herkese açık görünüme — okur ve anonim ziyaretçi.
/// Editör ve admin tam listeyi görmeye devam ediyor, çünkü iki hafta önce açtığı
/// taslağı bulup düzenleyebilmesi gerekiyor (bkz. GetAllArticlesQueryHandler).
///
/// KAPSAMA KATEGORİ VE ARAMA DA DAHİL: amaç "eski haber aranıp bulunmasın"
/// olduğu için pencere filtrelerden ÖNCE uygulanıyor.
///
/// DOĞRUDAN BAĞLANTIYI ETKİLEMEZ: <c>/api/article/{id}</c> ayrı bir sorgu.
/// Paylaşılmış bağlantılar ve arama motoru sonuçları çalışmaya devam ediyor;
/// kapatılsaydı dışarıdaki her bağlantı kırılır, site bozuk görünürdü.
///
/// AYARDAN GELİYOR, KODA GÖMÜLÜ DEĞİL: bu bir ürün parametresi, yayın temposu
/// değiştikçe yeniden derlemeden ayarlanabilmeli.
/// </summary>
public class FeedOptions
{
    public const string SectionName = "Feed";

    /// <summary>
    /// Akışta kaç günlük haber görünsün. 0 veya negatif verilirse pencere
    /// uygulanmaz — yani özellik kapatılmış olur.
    /// </summary>
    public int FreshnessDays { get; set; } = 7;
}
