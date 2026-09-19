using System.ComponentModel.DataAnnotations;

namespace SecretSanta.ApiService.DTOs
{
    public class UserGiftDto
    {
        public int UserGiftId { get; set; }
        [Required, StringLength(300)] public string GiftName { get; set; } = null!;
        [Required, StringLength(300)] public string DeliveryMethod { get; set; } = null!;
    }
    public class GameDto
    {
        public int GameId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsDrawn { get; set; }
        public int ParticipantCount { get; set; }
        public int GiftCost { get; set; }
    }

    public class GameListDto
    {
        public int GameId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsDrawn { get; set; }
        public bool IsAnonymous { get; set; }
        public int CreatorId { get; set; }
        public int GiftCost { get; set; }
        public List<string> Participants { get; set; } = new();
    }
    public class UserDto
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Email { get; set; }
    }

    public class RegisterRequest
    {
        [Required, StringLength(100, MinimumLength = 2)] public string DisplayName { get; set; } = null!;
        [StringLength(200)] public string? RealName { get; set; }
        [EmailAddress, StringLength(320)] public string? Email { get; set; }

        [Required, StringLength(200, MinimumLength = 8)] public string Password { get; set; } = null!;
        public bool IsAnonymous { get; set; } = false;
    }

    public class LoginRequest
    {
        [Required, StringLength(320)] public string Identifier { get; set; } = null!;
        [Required, StringLength(200, MinimumLength = 8)] public string Password { get; set; } = null!;
    }

    public class CreateGameRequest
    {
        [Required, StringLength(200)] public string Name { get; set; } = null!;
        [Range(0, int.MaxValue)]
        public int GiftCost { get; set; }
    }

    public class CreateWishRequest
    {
        [Required, StringLength(1000)]
        public string WishText { get; set; } = null!;
    }
}
