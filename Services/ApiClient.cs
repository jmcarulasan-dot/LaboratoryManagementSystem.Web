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

    public async Task<bool> SendSuccessAsync(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendAsync(method, path, body);
        return response.IsSuccessStatusCode;
    }
}
