using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity.Server.Keycloak.Services;
using Microsoft.Extensions.Options;

namespace Identity.Server.Keycloak.Services;

public class KeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakSettings _settings;
    private string _adminToken = string.Empty;

    public KeycloakService(HttpClient httpClient, IOptions<KeycloakSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    // 🔹 ADMIN TOKENNI OLISH
 public async Task<string> GetAdminTokenAsync()
{
    if (!string.IsNullOrEmpty(_adminToken)) return _adminToken;

    var content = new FormUrlEncodedContent(new[]
    {
       // new KeyValuePair<string, string>("client_id", "admin-cli"),
        //new KeyValuePair<string, string>("username", "wahidustoz"),
        //new KeyValuePair<string, string>("password", "fUchem-zytpaq-3hetwy"),

           new KeyValuePair<string, string>("client_id", _settings.ClientId),
        new KeyValuePair<string, string>("username", "admin"),
        new KeyValuePair<string, string>("password", "admin"),
        new KeyValuePair<string, string>("grant_type", "password")
    });

    var response = await _httpClient.PostAsync("http://auth.localhost.uz/realms/ilmhub/protocol/openid-connect/token", content);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"Admin token olishda xatolik: {response.StatusCode}, {error}");
    }

    var json = await response.Content.ReadAsStringAsync();

    // ✅ **TO‘G‘RI DESERIALIZE QILISH**
    var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(json);

    if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
    {
        throw new Exception("Admin token olinmadi, javobda token mavjud emas.");
    }

    _adminToken = tokenResponse.AccessToken;
    return _adminToken;
}

// 🔹 **Token response modeli**



    // 🔹 FOYDALANUVCHI QO‘SHISH VA ROLE BIRIKTIRISH
   public async Task<bool> CreateUserAsync(string username, string password, List<string> roles)
{
    var token = await GetAdminTokenAsync();
    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

   var userPayload = new
    {
        username,
        firstName = username,    
        lastName = username,     
        email = $"{username}@gmail.com", 
        enabled = true,          
        emailVerified = true,    
        credentials = new[]
        {
            new { type = "password", value = password, temporary = false }
        },
    };

    var content = new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json");
    var response = await _httpClient.PostAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/users", content);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"Foydalanuvchi yaratishda xatolik: {response.StatusCode}, {error}");
    }

    var usersResponse = await _httpClient.GetAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/users?username={username}");
    var usersJson = await usersResponse.Content.ReadAsStringAsync();
    var users = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(usersJson);

    if (users == null || users.Count == 0) return false;
    var userId = users[0]["id"].ToString();

    foreach (var role in roles)
    {
        var roleResponse = await _httpClient.GetAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/roles/{role}");
        var roleJson = await roleResponse.Content.ReadAsStringAsync();
        var roleObj = JsonSerializer.Deserialize<Dictionary<string, object>>(roleJson);

        if (roleObj == null) continue;

        var roleAssignContent = new StringContent(JsonSerializer.Serialize(new[] { roleObj }), Encoding.UTF8, "application/json");
        var roleAssignResponse = await _httpClient.PostAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}/role-mappings/realm", roleAssignContent);

        if (!roleAssignResponse.IsSuccessStatusCode)
        {
            var roleError = await roleAssignResponse.Content.ReadAsStringAsync();
            throw new Exception($"Ro‘l biriktirishda xatolik: {roleAssignResponse.StatusCode}, {roleError}");
        }
    }

    return true;
}


    // 🔹 FOYDALANUVCHI LOGIN QILISH
  public async Task<string?> LoginAsync(string username, string password)
{
    var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("client_id", _settings.ClientId),
        new KeyValuePair<string, string>("username", username),
        new KeyValuePair<string, string>("password", password),
        new KeyValuePair<string, string>("grant_type", "password")
    });

    var response = await _httpClient.PostAsync($"{_settings.Url}/realms/{_settings.Realm}/protocol/openid-connect/token", content);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"Foydalanuvchi login qilishda xatolik: {response.StatusCode}, {error}");
    }

    var json = await response.Content.ReadAsStringAsync();

    // ✅ **TO‘G‘RI DESERIALIZE QILISH**
    var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(json);

    if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
    {
        throw new Exception("Foydalanuvchi tokeni olinmadi, javobda token mavjud emas.");
    }

    return tokenResponse.AccessToken;
}

// 🔹 **Token response modeli**



    // 🔹 BARCHA FOYDALANUVCHILARNI OLISH
    public async Task<List<object>> GetUsersAsync()
    {
        var token = await GetAdminTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/users");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<object>>(json);
    }

    // 🔹 FOYDALANUVCHI O‘CHIRISH
    public async Task<bool> DeleteUserAsync(string userId)
    {
        var token = await GetAdminTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.DeleteAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}");
        return response.IsSuccessStatusCode;
    }

    // 🔹 ROLELARNI OLISH
    public async Task<List<object>> GetRolesAsync()
    {
        var token = await GetAdminTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/roles");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<object>>(json);
    }

public async Task<bool> UpdateUserAsync(string userId, string newUsername, string newPassword)
{
    var token = await GetAdminTokenAsync();
    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var userUpdatePayload = new
    {
        username = newUsername
    };

    var userUpdateContent = new StringContent(JsonSerializer.Serialize(userUpdatePayload), Encoding.UTF8, "application/json");
    var userUpdateResponse = await _httpClient.PutAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}", userUpdateContent);

    if (!userUpdateResponse.IsSuccessStatusCode)
    {
        var error = await userUpdateResponse.Content.ReadAsStringAsync();
        throw new Exception($"Foydalanuvchi ma'lumotlarini yangilashda xatolik: {userUpdateResponse.StatusCode}, {error}");
    }

    // 🔹 **Parolni yangilash**
    var passwordUpdatePayload = new
    {
        type = "password",
        value = newPassword,
        temporary = false
    };

    var passwordUpdateContent = new StringContent(JsonSerializer.Serialize(passwordUpdatePayload), Encoding.UTF8, "application/json");
    var passwordUpdateResponse = await _httpClient.PutAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}/reset-password", passwordUpdateContent);

    if (!passwordUpdateResponse.IsSuccessStatusCode)
    {
        var passwordError = await passwordUpdateResponse.Content.ReadAsStringAsync();
        throw new Exception($"Parolni yangilashda xatolik: {passwordUpdateResponse.StatusCode}, {passwordError}");
    }

    return true;
}


     public async Task<object?> GetUserByUsernameAsync(string username)
    {
        var token = await GetAdminTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync($"{_settings.Url}/admin/realms/{_settings.Realm}/users?username={username}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<object>>(json);

        return users.Any() ? users[0] : null;
    }
}


public class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
}