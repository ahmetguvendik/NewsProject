using System.Text.Json.Serialization;
using MediatR;
using NewsService.Application.Features.Commands.Article.Response;

namespace NewsService.Application.Features.Commands.Article.Request;

public class UpdateArticleCommand : IRequest<UpdateArticleResponse>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Makalenin nihai etiket kümesi. Gönderilmeyen mevcut etiketler kaldırılır,
    /// yeni gelenler eklenir. Boş liste tüm etiketleri temizler.
    /// </summary>
    public List<Guid> TagIds { get; set; } = [];

    // ── Aşağıdakiler body'den GELMEZ, controller JWT'den set eder ──────
    //
    // [JsonIgnore] burada bir güvenlik kontrolü: bu alanlar bağlanabilir olsaydı
    // herhangi bir editör isteğine "IsAdmin": true ekleyip aşağıdaki tüm kuralları
    // atlayabilirdi. Controller değerleri her hâlükârda üzerine yazıyor, ama alanın
    // dışarıdan hiç doldurulamaması daha güvenli bir varsayılan.

    /// <summary>İsteği yapan kullanıcının Keycloak kimliği.</summary>
    [JsonIgnore]
    public string EditorKeycloakId { get; set; } = string.Empty;

    /// <summary>
    /// İsteği yapan admin mi? Admin her haberi düzenleyebilir; editör yalnızca
    /// kendi yazdığı ve henüz yayınlanmamış haberi düzenleyebilir.
    /// </summary>
    [JsonIgnore]
    public bool EditorIsAdmin { get; set; }
}
