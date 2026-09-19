using System.ComponentModel.DataAnnotations;

namespace SecretSanta.ApiService.Services;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(5, 1440)]
    public int ExpiresMinutes { get; init; } = 60;
}
