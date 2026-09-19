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
    public void Uses_remote_address_even_when_caller_supplies_an_identity_header()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("172.20.0.3");
        context.Request.Headers["X-SecretSanta-Client-Id"] =
            "37f748b02f0e4f67b378a047619ee1e7";

        Assert.Equal("172.20.0.3",
            AuthenticationRateLimitPartition.GetPartitionKey(context));
    }

    [Fact]
    public void Uses_unknown_partition_when_remote_address_is_unavailable()
    {
        var context = new DefaultHttpContext();

        Assert.Equal("unknown", AuthenticationRateLimitPartition.GetPartitionKey(context));
    }
}
