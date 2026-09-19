using SecretSanta.ApiService;
using Microsoft.AspNetCore.Http;
using System.Net;
using Xunit;

namespace SecretSanta.ApiService.Tests;

public sealed class LoginIdentifierTests
{
    [Theory]
    [InlineData(" Alice ", "ALICE")]
    [InlineData("user@example.com", "USER@EXAMPLE.COM")]
    public void Normalize_trims_and_uses_invariant_uppercase(string value, string expected)
    {
        Assert.Equal(expected, LoginIdentifier.Normalize(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeOptional_returns_null_for_empty_values(string? value)
    {
        Assert.Null(LoginIdentifier.NormalizeOptional(value));
    }
}

public sealed class AuthenticationRateLimitPartitionTests
{
    [Fact]
    public void Uses_valid_browser_client_id_instead_of_web_container_address()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("172.20.0.3");
        context.Request.Headers[AuthenticationRateLimitPartition.ClientIdHeader] =
            "37f748b02f0e4f67b378a047619ee1e7";

        Assert.Equal("37f748b02f0e4f67b378a047619ee1e7",
            AuthenticationRateLimitPartition.GetPartitionKey(context));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-client-id")]
    public void Falls_back_to_remote_address_for_invalid_client_id(string clientId)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("172.20.0.3");
        context.Request.Headers[AuthenticationRateLimitPartition.ClientIdHeader] = clientId;

        Assert.Equal("172.20.0.3", AuthenticationRateLimitPartition.GetPartitionKey(context));
    }
}
