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
    private const string StudentTokenKey = "lms_student_token";

    public ApiClient(HttpClient http, IJSRuntime js, NavigationManager navigation)
    {
        _http = http;
        _js = js;
        _navigation = navigation;
    }

    public async Task<bool> LoginAdminAsync(string companyId, string password)
    {
        using var response = await _http.PostAsJsonAsync("api/login/admin",
            new { companyID = companyId, password });
        if (!response.IsSuccessStatusCode)
        {
            await LogoutAsync();
            return false;
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        if (!json.RootElement.TryGetProperty("token", out var tokenElement)) return false;
        var token = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token)) return false;

        await _js.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
        return true;
    }

    public async Task<bool> LoginStudentAsync(string studentNo, string password)
    {
        using var response = await _http.PostAsJsonAsync("api/login/student", new { studentNo, password });
        if (!response.IsSuccessStatusCode) return false;
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        if (!json.RootElement.TryGetProperty("token", out var tokenElement)) return false;
        var token = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token)) return false;
        await _js.InvokeVoidAsync("sessionStorage.setItem", StudentTokenKey, token);
        return true;
    }

    public async Task<HttpResponseMessage> SendStudentAsync(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", StudentTokenKey);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _http.SendAsync(request);
    }

    public async Task<T?> GetStudentAsync<T>(string path)
    {
        using var response = await SendStudentAsync(HttpMethod.Get, path);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public async Task<bool> StudentSendSuccessAsync(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendStudentAsync(method, path, body);
        return response.IsSuccessStatusCode;
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
