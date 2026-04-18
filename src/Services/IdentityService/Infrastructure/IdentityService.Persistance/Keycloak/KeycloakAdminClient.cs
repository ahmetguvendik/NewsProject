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

    public KeycloakAdminClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["Keycloak:BaseUrl"] ?? "http://localhost:8080";
        _realm = configuration["Keycloak:Realm"] ?? "news-portal";
        _adminClientId = configuration["Keycloak:AdminClientId"] ?? "admin-cli";
        _adminClientSecret = configuration["Keycloak:AdminClientSecret"] ?? string.Empty;
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

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _adminClientId,
            ["client_secret"] = _adminClientSecret
        };

        // AdminClientSecret boşsa username/password ile al
        if (string.IsNullOrEmpty(_adminClientSecret))
        {
            formData["grant_type"] = "password";
            formData["username"] = "admin";
            formData["password"] = "admin";
        }

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
