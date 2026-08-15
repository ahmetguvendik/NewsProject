using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Queries.Article.Response;
using Shared.Models;

namespace NewsService.Application.Features.Queries.Article.Request;

/// <summary>
/// Akış sayfasının sorgusu — projedeki en çok istek alan uç.
///
/// Sayfa ve kategori kombinasyonlarının her biri ayrı bir yanıt üretiyor;
/// bir haber değiştiğinde hepsi birden bayatlıyor. Bu yüzden varyantlar tek
/// hash'in alanları olarak tutuluyor ve makale komutları tek <c>DEL</c> ile
/// tamamını düşürüyor.
/// </summary>
public class GetAllArticlesQuery : IRequest<PagedResult<GetAllArticlesResponse>>, IHashCacheableQuery
{
    /// <summary>Yalnızca editor/admin için true — taslaklar da listeye dahil edilir.</summary>
    public bool IncludeUnpublished { get; set; }

    /// <summary>Verilirse yalnızca bu kategori adına sahip makaleler döner.</summary>
    public string? Category { get; set; }

    /// <summary>Verilirse başlık/özette alt-dize eşleşmesi aranır (case-insensitive).</summary>
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string HashKey => CacheKeys.ArticleLists;

    /// <summary>
    /// Yanıtı değiştiren her parametre alan adında yer alır.
    ///
    /// Öndeki <c>tumu</c>/<c>yayin</c> ayrımı isteğe bağlı değil: taslak gören bir
    /// yanıt ile görmeyen aynı alana yazılırsa, yetkili kullanıcının listesi
    /// anonim ziyaretçiye servis edilir. Daha önce kapatılan taslak sızıntısını
    /// önbellek sessizce geri getirirdi.
    ///
    /// Arama sorguları önbelleklenmez: her ziyaretçi farklı şey aradığı için
    /// anahtar uzayı sınırsız büyür, isabet oranı ise neredeyse sıfırdır.
    /// </summary>
    public string? Field => string.IsNullOrWhiteSpace(Search)
        ? $"{(IncludeUnpublished ? "tumu" : "yayin")}:kategori={Category ?? "-"}:sayfa={Page}:boyut={PageSize}"
        : null;

    public TimeSpan Duration => TimeSpan.FromMinutes(5);
}
