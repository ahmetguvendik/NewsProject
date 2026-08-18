namespace NewsService.Application.Media;

/// <summary>
/// Kapak görselleri birden fazla genişlikte üretilir; tarayıcı ekranda kapladığı
/// yere göre uygun olanı indirir. Kart 300 piksel genişliğindeyken 1200 piksellik
/// dosyayı indirmek, verinin çoğunu ekrana hiç ulaşmadan çöpe atmak demek.
///
/// Anahtar şeması: veritabanında <b>uzantısız temel anahtar</b> saklanır
/// (<c>articles/2026/08/&lt;guid&gt;</c>), dosyalar ise <c>&lt;temel&gt;-400.webp</c>
/// biçiminde durur. Uzantısı olan eski kayıtlar tek dosyalı sayılır ve olduğu gibi
/// servis edilir; böylece varyant öncesi yüklenen görseller kırılmaz.
/// </summary>
public static class MediaVariants
{
    /// <summary>Küçükten büyüğe: liste kartı, tablet/detay, geniş ekran manşet.</summary>
    public static readonly int[] Widths = [400, 800, 1200];

    /// <summary>
    /// srcset verilmediğinde ve tek adres gerektiğinde kullanılan boy.
    /// Ortadaki değer: küçük ekranda fazla, büyük ekranda kabul edilebilir.
    /// </summary>
    public const int DefaultWidth = 800;

    public const string Extension = ".webp";

    public static string FileKey(string baseKey, int width) => $"{baseKey}-{width}{Extension}";

    /// <summary>
    /// Varyantlı yeni şema mı, yoksa tek dosyalı eski kayıt mı?
    /// Ayrım uzantıdan yapılır: yeni temel anahtarlar uzantısızdır.
    /// </summary>
    public static bool HasVariants(string key) => !Path.HasExtension(key);
}
