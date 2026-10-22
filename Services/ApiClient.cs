using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace LaboratoryManagementSystem.Web.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly NavigationManager _navigation;
    private const string TokenKey = "lms_admin_token";

    public ApiClient(HttpClient http, IJSRuntime js, NavigationManager navigation)
    {
        _http = http;
        _js = js;
        _navigation = navigation;
    }

    public async Task<AdminLoginChallenge> BeginAdminLoginAsync(string companyId, string password)
    {
        using var response = await _http.PostAsJsonAsync("api/login/admin",
            new { companyID = companyId, password });
        if (!response.IsSuccessStatusCode)
        {
            await LogoutAsync();
            return new AdminLoginChallenge(false, null, null, false, null, await ReadErrorAsync(response));
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        var challengeToken = ReadString(root, "challengeToken");
        if (string.IsNullOrWhiteSpace(challengeToken))
            return new AdminLoginChallenge(false, null, null, false, null, "The API did not return an MFA challenge.");

        return new AdminLoginChallenge(
            true,
            ReadString(root, "message"),
            challengeToken,
            ReadBoolean(root, "requiresEnrollment"),
            ReadString(root, "authenticatorSetupUri"),
            null);
    }

    public async Task<(bool Success, string? Error)> VerifyAdminMfaAsync(string challengeToken, string code)
    {
        using var response = await _http.PostAsJsonAsync("api/login/verify-mfa", new { challengeToken, code });
        if (!response.IsSuccessStatusCode)
            return (false, await ReadErrorAsync(response));

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var token = ReadString(json.RootElement, "token");
        if (string.IsNullOrWhiteSpace(token))
            return (false, "The API verified the code but did not return a sign-in token.");

        await _js.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
        return (true, null);
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool ReadBoolean(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return $"Sign-in failed ({(int)response.StatusCode}).";
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            foreach (var name in new[] { "message", "detail", "title" })
                if (ReadString(root, name) is { Length: > 0 } message) return message;
        }
        catch (JsonException) { }
        return body.Trim().Trim('"');
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
        using var request = new HttpRequestMessage(method, path);
        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await LogoutAsync();
            _navigation.NavigateTo("/login", replace: true);
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }
        return response;
    }

    public async Task<HttpResponseMessage> UploadFileAsync(HttpMethod method, string path, string fieldName, IBrowserFile file, long maxAllowedSize)
    {
        using var request = new HttpRequestMessage(method, path);
        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await using var stream = file.OpenReadStream(maxAllowedSize);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, fieldName, Path.GetFileName(file.Name));
        request.Content = content;

        var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await LogoutAsync();
            _navigation.NavigateTo("/login", replace: true);
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }
        return response;
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

public sealed record AdminLoginChallenge(
    bool Success,
    string? Message,
    string? ChallengeToken,
    bool RequiresEnrollment,
    string? AuthenticatorSetupUri,
    string? Error);
