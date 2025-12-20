using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecretSanta.ApiService
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<UserGame> UserGames { get; set; }
        public DbSet<Wish> Wishes { get; set; }
        public DbSet<SantaAssignment> SantaAssignments { get; set; }
        public DbSet<AnonGuestCode> AnonGuestCodes { get; set; }
        public DbSet<DeliveryPoint> DeliveryPoints { get; set; }
        public DbSet<GameInvite> GameInvites { get; set; }
        public DbSet<UserGift> UserGifts { get; set; }

    }

    public class Game
    {
        [Key]
        public int GameId { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDrawn { get; set; } = false;
        public int CreatorId { get; set; }
        public int GiftCost { get; set; }
        public User Creator { get; set; } = null!;
        public ICollection<UserGame> UserGames { get; set; } = new List<UserGame>();
        public List<GameInvite> Invites { get; set; } = new();
    }
    public class UserGift
    {
        [Key]
        public int UserGiftId { get; set; }
        public int GameId { get; set; }
        public Game Game { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string GiftName { get; set; } = null!;
        public string DeliveryMethod { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Required]
        public string DisplayName { get; set; } = null!;
        public string? RealName { get; set; }
        public string? Email { get; set; }
        public bool IsAnonymous { get; set; } = true;
        public string? PasswordHash { get; set; }
        public ICollection<UserGame> UserGames { get; set; } = new List<UserGame>();
        public ICollection<Wish> Wishes { get; set; } = new List<Wish>();
    }

    [PrimaryKey(nameof(UserId), nameof(GameId))]
    public class UserGame
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int GameId { get; set; }
        public Game Game { get; set; } = null!;
    }

    public class DeliveryPoint
    {
        [Key]
        public int DeliveryPointId { get; set; }
        [Required]
        public string Code { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class Wish
    {
        [Key]
        public int WishId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public string WishText { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }

    public class SantaAssignment
    {
        [Key]
        public int AssignmentId { get; set; }
        [Required]
        public int GiverUserId { get; set; }
        [Required]
        public int ReceiverUserId { get; set; }
        public int GameId { get; set; }
        public int? SelectedGiftId { get; set; }
        public UserGift? SelectedGift { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey("GiverUserId")]
        public User? Giver { get; set; }
        [ForeignKey("ReceiverUserId")]
        public User? Receiver { get; set; }
    }

    public class AnonGuestCode
    {
        [Key]
        public int CodeId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public string AccessCode { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }

    public class GameInvite
    {
        [Key]
        public int GameInviteId { get; set; }
        public int GameId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Game Game { get; set; } = null!;
    }


}