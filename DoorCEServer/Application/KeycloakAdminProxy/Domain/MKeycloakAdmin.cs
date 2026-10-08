using System.Text.Json;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.KeycloakAdminProxy.Dtos;
using DoorCEServer.Application.KeycloakAdminProxy.Interfaces;
// ReSharper disable InconsistentNaming

namespace DoorCEServer.Application.KeycloakAdminProxy.Domain;

public class KeycloakTokenResponse
{
    public string? access_token { get; set; }
    public string? refresh_token { get; set; }
    public int expires_in { get; set; }
    public int refresh_expires_in { get; set; }
    public string? token_type { get; set; }
    public string? scope { get; set; }
}

public class MKeycloakAdmin : IKeycloakAdmin
{
    private static readonly SemaphoreSlim TokenSemaphore = new(1, 1);
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;
    
    private string _accessToken = "";
    private string _refreshToken = "";
    
    
    
    private static readonly JsonSerializerOptions serializerOptions = new()
        { PropertyNameCaseInsensitive = true };
    
    private HttpClient _keycloakClient => _httpClientFactory.CreateClient("KeycloakAdmin");
    
    public MKeycloakAdmin(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        
        _realm = configuration["Keycloak:Realm"] ??
                 throw new Exception("No Keycloak realm in configuration");
        _clientId = configuration["KeycloakServer:Resource"] ??
                    throw new Exception("No Keycloak clientId in configuration");
        _clientSecret = configuration["KeycloakServer:ClientSecret"] ??
                        throw  new Exception("No Keycloak clientSecret in configuration");
        GetTokenWithClientSecret();
    }
    
    public bool CheckUserExists(string userId)
    {
        return GetFromKeycloak(userId, false).GetAwaiter().GetResult().Any();
    }

    public IEnumerable<string> GetUserIds(string? query = null)
    {
        ICollection<AccountDto> users = GetFromKeycloak(query, true).GetAwaiter().GetResult();
        return users.Select(u => u.Username).ToList();
    }

    public void CreateUser(XPerson xPerson)
    {
        var user = new AccountDto {
            Username = xPerson.UserId ?? "",
            Email = !string.IsNullOrEmpty(xPerson.UserEmail) ? xPerson.UserEmail : null,
            FirstName = xPerson.GivenNames.FirstOrDefault(),
            LastName = xPerson.FamilyName,
            Enabled = true,
            Credentials = string.IsNullOrEmpty(xPerson.UserPassword) ? null :
            [
                new CredentialDto {
                    Type = "password",
                    Value = xPerson.UserPassword!,
                    Temporary = false
                }
            ]
        };
        PostToKeycloak(user).GetAwaiter().GetResult();
    }

    private async Task<ICollection<AccountDto>> GetFromKeycloak(string? query, bool isSearch)
    {
        string endpoint = $"admin/realms/{_realm}/users";
        if (!string.IsNullOrEmpty(query))
            endpoint += (isSearch ? "?search=*" : "?username=") + $"{Uri.EscapeDataString(query)}";

        // Send a GET request to Keycloak
        List<AccountDto>? users;
        try {
            using HttpResponseMessage response = await _keycloakClient.SendAsync(CreateRequest(HttpMethod.Get, endpoint));
            response.EnsureSuccessStatusCode();
            users = await response.Content.ReadFromJsonAsync<List<AccountDto>>(serializerOptions);
        } catch {
            GetTokenWithClientSecret();
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_refreshToken))
                return new List<AccountDto>();
            using HttpResponseMessage response = await _keycloakClient.SendAsync(CreateRequest(HttpMethod.Get, endpoint));
            response.EnsureSuccessStatusCode();
            users = await response.Content.ReadFromJsonAsync<List<AccountDto>>(serializerOptions);
        }
        
        return users ?? new List<AccountDto>();
    }
    
    private async Task PostToKeycloak(AccountDto user)
    {
        string endpoint = $"admin/realms/{_realm}/users";
        
        // Send a POST request to Keycloak
        try {
            using HttpResponseMessage response = 
                await _keycloakClient.SendAsync(
                    CreateRequest(HttpMethod.Post, endpoint, JsonContent.Create(user))
                );
            response.EnsureSuccessStatusCode();
        } catch {
            GetTokenWithClientSecret();
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_refreshToken))
                throw;
            using HttpResponseMessage response = 
                await _keycloakClient.SendAsync(
                    CreateRequest(HttpMethod.Post, endpoint, JsonContent.Create(user))
                );
            response.EnsureSuccessStatusCode();
        }
    }

    private void GetTokenWithClientSecret()
    {
        using FormUrlEncodedContent content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        });
        
        TokenSemaphore.Wait();
        
        try {
            using HttpResponseMessage response = _keycloakClient
                .PostAsync($"realms/{_realm}/protocol/openid-connect/token", content)
                .GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            
            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var tokens = JsonSerializer.Deserialize<KeycloakTokenResponse>(json);
            _accessToken = tokens?.access_token ?? string.Empty;
            _refreshToken = tokens?.refresh_token ?? string.Empty;
            TokenSemaphore.Release();
        } catch {
            _accessToken = String.Empty;
            _refreshToken = String.Empty;
            TokenSemaphore.Release();
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        if (null != content) request.Content = content;
        if (!string.IsNullOrEmpty(_accessToken))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        return request;
    }
    
}