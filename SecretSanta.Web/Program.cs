using SecretSanta.Web.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

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
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = new[] { new CultureInfo("ru-RU"), new CultureInfo("en-US") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("ru-RU");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddOutputCache();

var apiBaseUrl = builder.Configuration["Services:Api:BaseUrl"] ?? "http://apiservice";
builder.Services.AddHttpClient("api", client =>
    client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute));

var app = builder.Build();

app.UseRequestLocalization();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Keep the conventional static-file middleware as a fallback for framework assets.
// Interactive components cannot start if _framework/blazor.web.js isn't served.
app.UseStaticFiles();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapGet("/culture/set", (string culture, string? redirectUri, HttpContext context) =>
{
    if (!supportedCultures.Any(item => item.Name == culture))
    {
        return Results.BadRequest();
    }

    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });

    return Results.LocalRedirect(string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
