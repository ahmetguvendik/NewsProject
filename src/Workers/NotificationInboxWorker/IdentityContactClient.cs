using System.Net;
using System.Net.Http.Json;

namespace NotificationInboxWorker;

public record UserContact(string Email, string FirstName, string LastName);

public interface IIdentityContactClient
{
    /// <summary>Kullanıcı bulunamazsa null döner. Diğer her hata (401, timeout, 5xx) fırlatılır.</summary>
    Task<UserContact?> GetContactAsync(string keycloakId, CancellationToken cancellationToken = default);

    /// <summary>Bültene abone, aktif kullanıcılar. Hiç abone yoksa boş liste döner.</summary>
    Task<List<UserContact>> GetSubscribersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// IdentityService'in Internal:ApiKey ile korunan dahili uçlarını çağırır.
/// Yayın bildiriminde hem yazarın hem de bülten abonelerinin gerçek e-posta
/// adreslerini çözmek için kullanılır.
/// </summary>
public class IdentityContactClient : IIdentityContactClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public IdentityContactClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Internal:ApiKey"] ?? string.Empty;
    }

    public async Task<UserContact?> GetContactAsync(string keycloakId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"/api/user/internal/contact?keycloakId={Uri.EscapeDataString(keycloakId)}");
        request.Headers.Add("X-Internal-Api-Key", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        // 401 (yanlış/eksik anahtar), 5xx, bağlantı hatası vb. → çağıran tarafın
        // retry/dead-letter mekanizmasına bırakılır.
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ContactDto>(cancellationToken)
            ?? throw new InvalidOperationException("IdentityService /internal/contact boş içerik döndürdü.");

        return new UserContact(body.Email, body.FirstName, body.LastName);
    }

    public async Task<List<UserContact>> GetSubscribersAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/user/internal/subscribers");
        request.Headers.Add("X-Internal-Api-Key", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<List<ContactDto>>(cancellationToken) ?? [];

        return body.Select(c => new UserContact(c.Email, c.FirstName, c.LastName)).ToList();
    }

    private sealed record ContactDto(string KeycloakId, string Email, string FirstName, string LastName);
}
