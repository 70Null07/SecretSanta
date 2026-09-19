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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(user => user.NormalizedDisplayName)
                .IsUnique();

            modelBuilder.Entity<User>().Property(user => user.DisplayName).HasMaxLength(100);
            modelBuilder.Entity<User>().Property(user => user.NormalizedDisplayName).HasMaxLength(100);
            modelBuilder.Entity<User>().Property(user => user.RealName).HasMaxLength(200);
            modelBuilder.Entity<User>().Property(user => user.Email).HasMaxLength(320);
            modelBuilder.Entity<User>().Property(user => user.NormalizedEmail).HasMaxLength(320);

            modelBuilder.Entity<User>()
                .HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasFilter("\"NormalizedEmail\" IS NOT NULL AND \"NormalizedEmail\" <> ''");

            modelBuilder.Entity<GameInvite>()
                .HasIndex(invite => invite.Token)
                .IsUnique();

            // CreateInvite returns one stable invite per game. The primary key does not
            // protect that invariant when requests race.
            modelBuilder.Entity<GameInvite>()
                .HasIndex(invite => invite.GameId)
                .IsUnique();

            modelBuilder.Entity<DeliveryPoint>()
                .HasIndex(point => point.Code)
                .IsUnique();

            modelBuilder.Entity<AnonGuestCode>()
                .HasIndex(code => code.AccessCode)
                .IsUnique();

            modelBuilder.Entity<SantaAssignment>()
                .HasIndex(assignment => new { assignment.GameId, assignment.GiverUserId })
                .IsUnique();

            modelBuilder.Entity<SantaAssignment>()
                .HasIndex(assignment => new { assignment.GameId, assignment.ReceiverUserId })
                .IsUnique();

            modelBuilder.Entity<Game>().Property(game => game.Name).HasMaxLength(200);
            modelBuilder.Entity<Game>()
                .HasOne(game => game.Creator)
                .WithMany()
                .HasForeignKey(game => game.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserGift>().Property(gift => gift.GiftName).HasMaxLength(300);
            modelBuilder.Entity<UserGift>().Property(gift => gift.DeliveryMethod).HasMaxLength(300);
            modelBuilder.Entity<UserGift>()
                .HasOne(gift => gift.Game)
                .WithMany()
                .HasForeignKey(gift => gift.GameId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserGift>()
                .HasOne(gift => gift.User)
                .WithMany()
                .HasForeignKey(gift => gift.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wish>().Property(wish => wish.WishText).HasMaxLength(1000);
            modelBuilder.Entity<DeliveryPoint>().Property(point => point.Code).HasMaxLength(100);
            modelBuilder.Entity<DeliveryPoint>().Property(point => point.Description).HasMaxLength(500);
            modelBuilder.Entity<AnonGuestCode>().Property(code => code.AccessCode).HasMaxLength(100);
            modelBuilder.Entity<GameInvite>().Property(invite => invite.Token).HasMaxLength(100);

            modelBuilder.Entity<SantaAssignment>()
                .HasOne(assignment => assignment.Game)
                .WithMany()
                .HasForeignKey(assignment => assignment.GameId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SantaAssignment>()
                .HasOne(assignment => assignment.Giver)
                .WithMany()
                .HasForeignKey(assignment => assignment.GiverUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SantaAssignment>()
                .HasOne(assignment => assignment.Receiver)
                .WithMany()
                .HasForeignKey(assignment => assignment.ReceiverUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SantaAssignment>()
                .HasOne(assignment => assignment.SelectedGift)
                .WithMany()
                .HasForeignKey(assignment => assignment.SelectedGiftId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            NormalizeLoginIdentifiers();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            NormalizeLoginIdentifiers();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void NormalizeLoginIdentifiers()
        {
            foreach (var entry in ChangeTracker.Entries<User>()
                         .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                entry.Entity.NormalizedDisplayName = LoginIdentifier.Normalize(entry.Entity.DisplayName);
                entry.Entity.NormalizedEmail = LoginIdentifier.NormalizeOptional(entry.Entity.Email);
            }
        }

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
        [Required]
        public string NormalizedDisplayName { get; set; } = null!;
        public string? RealName { get; set; }
        public string? Email { get; set; }
        public string? NormalizedEmail { get; set; }
        public bool IsAnonymous { get; set; } = true;
        public string? PasswordHash { get; set; }
        public ICollection<UserGame> UserGames { get; set; } = new List<UserGame>();
        public ICollection<Wish> Wishes { get; set; } = new List<Wish>();
    }

    public static class LoginIdentifier
    {
        public static string Normalize(string value) => value.Trim().ToUpperInvariant();

        public static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
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
        public Game Game { get; set; } = null!;
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
