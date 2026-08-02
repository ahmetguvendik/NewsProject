using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Shared.Exceptions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IdentityService.Persistance.Keycloak;

public class KeycloakAdminClient : IKeycloakAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly string _realm;
    private readonly string _adminClientId;
    private readonly string _adminClientSecret;
    private readonly string _baseUrl;

    private readonly string _adminUsername;
    private readonly string _adminPassword;

    public KeycloakAdminClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["Keycloak:BaseUrl"] ?? "http://localhost:8080";
        _realm = configuration["Keycloak:Realm"] ?? "news-portal";
        _adminClientId = configuration["Keycloak:AdminClientId"] ?? "admin-cli";
        _adminClientSecret = configuration["Keycloak:AdminClientSecret"] ?? string.Empty;
        _adminUsername = configuration["Keycloak:AdminUsername"] ?? "admin";
        _adminPassword = configuration["Keycloak:AdminPassword"] ?? "admin";
    }

    public async Task<string> CreateUserAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userPayload = new
        {
            username = email,
            email = email,
            firstName = firstName,
            lastName = lastName,
            enabled = true,
            credentials = new[]
            {
                new { type = "password", value = password, temporary = false }
            }
        };

        var json = JsonSerializer.Serialize(userPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/admin/realms/{_realm}/users", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadErrorAsync(response, cancellationToken);

            throw response.StatusCode switch
            {
                // Aynı e-posta/kullanıcı adı zaten kayıtlı
                HttpStatusCode.Conflict => ConflictException.UserAlreadyExists(email),

                // Parola politikası, geçersiz e-posta formatı vb. — istemcinin düzeltebileceği hatalar
                HttpStatusCode.BadRequest => new ValidationException("request", detail),

                _ => ExternalServiceException.Keycloak(
                    ErrorCodes.Keycloak.UserCreationFailed, "kullanıcı oluşturma", detail)
            };
        }

        // Keycloak oluşturulan kullanıcının Location header'ında ID'yi döner
        var location = response.Headers.Location?.ToString()
            ?? throw ExternalServiceException.Keycloak(
                ErrorCodes.Keycloak.UserCreationFailed,
                "kullanıcı oluşturma",
                "Yanıtta Location header'ı yok, oluşturulan kullanıcının ID'si okunamadı.");

        return location.Split('/').Last();
    }

    public async Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.DeleteAsync(
            $"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}", cancellationToken);

        // Kullanıcı zaten yoksa silme işlemi amacına ulaşmış sayılır (idempotent)
        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        if (!response.IsSuccessStatusCode)
        {
            throw ExternalServiceException.Keycloak(
                ErrorCodes.Keycloak.UserDeletionFailed,
                "kullanıcı silme",
                await ReadErrorAsync(response, cancellationToken));
        }
    }

    public async Task AssignRoleAsync(string keycloakId, string roleName, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Rol bilgisini Keycloak'tan al (id + name gerekiyor)
        var roleResponse = await _httpClient.GetAsync(
            $"{_baseUrl}/admin/realms/{_realm}/roles/{roleName}", cancellationToken);

        if (!roleResponse.IsSuccessStatusCode)
        {
            // Rol adı Application katmanında zaten doğrulandı; burada yoksa realm
            // yapılandırması eksik demektir — istemcinin düzeltebileceği bir hata değil.
            throw ExternalServiceException.Keycloak(
                ErrorCodes.Keycloak.RoleNotFound,
                "rol sorgulama",
                $"'{roleName}' rolü realm'de tanımlı değil: {await ReadErrorAsync(roleResponse, cancellationToken)}");
        }

        var roleBody = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
        var roleJson = JsonDocument.Parse(roleBody);
        var roleId = roleJson.RootElement.GetProperty("id").GetString();
        var roleNameFromKeycloak = roleJson.RootElement.GetProperty("name").GetString();

        // 2. Kullanıcıya rolü ata
        var payload = JsonSerializer.Serialize(new[]
        {
            new { id = roleId, name = roleNameFromKeycloak }
        });

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var assignResponse = await _httpClient.PostAsync(
            $"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm",
            content, cancellationToken);

        if (!assignResponse.IsSuccessStatusCode)
        {
            throw ExternalServiceException.Keycloak(
                ErrorCodes.Keycloak.RoleAssignFailed,
                "rol atama",
                await ReadErrorAsync(assignResponse, cancellationToken));
        }
    }

    public async Task DisableUserAsync(string keycloakId, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Keycloak'ta kullanıcıyı enabled=false yaparak devre dışı bırak
        var payload = JsonSerializer.Serialize(new { enabled = false });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(
            $"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}", content, cancellationToken);

        // Kullanıcı Keycloak'ta zaten yoksa devre dışı bırakılacak bir şey de yok
        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        if (!response.IsSuccessStatusCode)
        {
            throw ExternalServiceException.Keycloak(
                ErrorCodes.Keycloak.UserDisableFailed,
                "kullanıcı devre dışı bırakma",
                await ReadErrorAsync(response, cancellationToken));
        }
    }

    public async Task EnableUserAsync(string keycloakId, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Keycloak'ta kullanıcıyı enabled=true yaparak tekrar aktif et
        var payload = JsonSerializer.Serialize(new { enabled = true });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(
            $"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}", content, cancellationToken);

        // Kullanıcı Keycloak'ta zaten yoksa aktif edilecek bir şey de yok
        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        if (!response.IsSuccessStatusCode)
        {
            throw ExternalServiceException.Keycloak(
                ErrorCodes.Keycloak.UserEnableFailed,
                "kullanıcı aktif etme",
                await ReadErrorAsync(response, cancellationToken));
        }
    }

    public async Task RemoveRoleAsync(string keycloakId, string roleName, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Rol bilgisini Keycloak'tan al (id + name gerekiyor)
        var roleResponse = await _httpClient.GetAsync(
            $"{_baseUrl}/admin/realms/{_realm}/roles/{roleName}", cancellationToken);

        if (!roleResponse.IsSuccessStatusCode)
            return; // Rol zaten yoksa sessizce geç

        var roleBody = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
        var roleJson = JsonDocument.Parse(roleBody);
        var roleId = roleJson.RootElement.GetProperty("id").GetString();
        var roleNameFromKeycloak = roleJson.RootElement.GetProperty("name").GetString();

        var payload = JsonSerializer.Serialize(new[]
        {
            new { id = roleId, name = roleNameFromKeycloak }
        });

        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        await _httpClient.SendAsync(request, cancellationToken);
        // Hata durumunda sessizce devam et — bu zaten compensating transaction
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        // admin-cli is a public client in Keycloak's master realm — it only supports
        // the password grant. client_credentials requires a confidential client with
        // service accounts enabled, which admin-cli is not by default.
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _adminClientId,
            ["username"] = _adminUsername,
            ["password"] = _adminPassword
        };

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(formData),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw ExternalServiceException.Keycloak(
                ErrorCodes.Keycloak.AdminAuthFailed,
                "admin token alma",
                await ReadErrorAsync(response, cancellationToken));
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("access_token").GetString()
            ?? throw ExternalServiceException.Keycloak(
                ErrorCodes.Keycloak.AdminAuthFailed,
                "admin token alma",
                "Yanıtta 'access_token' alanı bulunamadı.");
    }

    /// <summary>
    /// Keycloak hata yanıtından okunabilir mesajı çıkarır.
    /// Keycloak duruma göre 'errorMessage', 'error_description' veya 'error' alanını döner;
    /// hiçbiri yoksa ham gövde kullanılır.
    /// </summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var root = JsonDocument.Parse(body).RootElement;

            foreach (var field in new[] { "errorMessage", "error_description", "error" })
            {
                if (root.TryGetProperty(field, out var value) &&
                    value.ValueKind == JsonValueKind.String &&
                    value.GetString() is { Length: > 0 } message)
                {
                    return message;
                }
            }
        }
        catch (JsonException)
        {
            // JSON değilse ham gövdeyi kullan
        }

        return string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body;
    }
}
