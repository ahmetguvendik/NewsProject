using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
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
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Keycloak user creation failed: {error}");
        }

        // Keycloak oluşturulan kullanıcının Location header'ında ID'yi döner
        var location = response.Headers.Location?.ToString()
            ?? throw new Exception("Keycloak did not return a user location header.");

        return location.Split('/').Last();
    }

    public async Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.DeleteAsync(
            $"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Keycloak user deletion failed: {error}");
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
            var err = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Keycloak role not found '{roleName}': {err}");
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
            var err = await assignResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Keycloak role assignment failed: {err}");
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

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Keycloak user disable failed: {error}");
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

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("access_token").GetString()
            ?? throw new Exception("Could not retrieve Keycloak admin token.");
    }
}
