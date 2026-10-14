using LaboratoryManagementSystem.Web;
using LaboratoryManagementSystem.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMudServices();

var configuredApiBaseUrl = builder.Configuration["ApiBaseUrl"];
var apiBaseUri = Uri.TryCreate(configuredApiBaseUrl, UriKind.Absolute, out var configuredApiUri)
    ? configuredApiUri
    : CreateLocalApiBaseUri(builder.HostEnvironment.BaseAddress);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseUri });
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();

static Uri CreateLocalApiBaseUri(string webBaseAddress)
{
    var webUri = new Uri(webBaseAddress);
    return new UriBuilder(webUri)
    {
        Port = webUri.Scheme == Uri.UriSchemeHttps ? 7115 : 5106,
        Path = "/",
        Query = string.Empty,
        Fragment = string.Empty
    }.Uri;
}
