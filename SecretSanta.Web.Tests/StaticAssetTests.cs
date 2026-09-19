using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SecretSanta.Web.Tests;

public sealed class StaticAssetTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public StaticAssetTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task BlazorBootScriptIsAvailable()
    {
        using var response = await client.GetAsync("/_framework/blazor.web.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/javascript", response.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
    }
}
