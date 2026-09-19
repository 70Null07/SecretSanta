using SecretSanta.ApiService;
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
