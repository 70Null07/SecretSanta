using SecretSanta.Web.Components;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("SecretSanta.Web")
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
}

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

var apiBaseUrl = builder.Configuration["Services:Api:BaseUrl"] ?? "http://apiservice";
builder.Services.AddHttpClient("api", client =>
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
