using System.ComponentModel.DataAnnotations;

namespace SecretSanta.ApiService.DTOs
{
    public class UserGiftDto
    {
        public int UserGiftId { get; set; }
        public string GiftName { get; set; } = null!;
        public string DeliveryMethod { get; set; } = null!;
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
        [Required] public string DisplayName { get; set; } = null!;
        public string? RealName { get; set; }
        [EmailAddress] public string? Email { get; set; }

        [Required] public string Password { get; set; } = null!;
        public bool IsAnonymous { get; set; } = false;
    }

    public class LoginRequest
    {
        [Required] public string Identifier { get; set; } = null!;
        [Required] public string Password { get; set; } = null!;
    }

    public class CreateGameRequest
    {
        public string Name { get; set; } = null!;
        public int GiftCost { get; set; }
    }

    public class CreateWishRequest
    {
        [Required]
        public string WishText { get; set; } = null!;
    }
}
