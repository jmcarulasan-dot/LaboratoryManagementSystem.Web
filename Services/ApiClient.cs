using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace LaboratoryManagementSystem.Web.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private const string TokenKey = "lms_admin_token";
    private const string CompanyKey = "lms_admin_company";

    public ApiClient(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public async Task<bool> LoginAdminAsync(string companyId, string password)
    {
        var response = await _http.PostAsJsonAsync("api/login/admin",
            new { companyID = companyId, password });
        if (!response.IsSuccessStatusCode) return false;

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        if (!json.RootElement.TryGetProperty("token", out var tokenElement)) return false;
        var token = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token)) return false;

        await _js.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
        await _js.InvokeVoidAsync("sessionStorage.setItem", CompanyKey, companyId);
        return true;
    }

    public async Task<bool> HasAdminSessionAsync()
    {
        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("sessionStorage.removeItem", CompanyKey);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _http.SendAsync(request);
    }

    public async Task<T?> GetAsync<T>(string path)
    {
        using var response = await SendAsync(HttpMethod.Get, path);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    /// <summary>GET a JSON array. Returns null when the request fails so pages can show an error.</summary>
    public async Task<List<JsonElement>?> GetListAsync(string path)
    {
        var element = await GetAsync<JsonElement>(path);
        if (element.ValueKind != JsonValueKind.Array) return null;
        return element.EnumerateArray().ToList();
    }

    public async Task<bool> SendSuccessAsync(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendAsync(method, path, body);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Send a request and return success plus a readable message from the API.</summary>
    public async Task<(bool Ok, string Message)> SendForResultAsync(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendAsync(method, path, body);
        var text = await response.Content.ReadAsStringAsync();
        return (response.IsSuccessStatusCode, ExtractMessage(text, response.IsSuccessStatusCode ? "Done." : $"Request failed ({(int)response.StatusCode})."));
    }

    public async Task<JsonElement> GetAdminProfileAsync()
    {
        var companyId = await _js.InvokeAsync<string?>("sessionStorage.getItem", CompanyKey);
        if (string.IsNullOrWhiteSpace(companyId)) return default;
        return await GetAsync<JsonElement>($"api/admin/company/{Uri.EscapeDataString(companyId)}");
    }

    public static string ExtractMessage(string body, string fallback)
    {
        if (string.IsNullOrWhiteSpace(body)) return fallback;
        var text = body.Trim();
        if (text.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errors.EnumerateObject())
                        if (field.Value.ValueKind == JsonValueKind.Array && field.Value.GetArrayLength() > 0)
                            return field.Value[0].GetString() ?? fallback;
                }
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    return message.GetString() ?? fallback;
                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    return title.GetString() ?? fallback;
            }
            catch (JsonException) { }
            return fallback;
        }
        return text.Trim('"');
    }
}
