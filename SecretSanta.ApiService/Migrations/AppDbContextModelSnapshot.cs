using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace SecretSanta.ApiService.Migrations;

[DbContext(typeof(AppDbContext))]
public sealed class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");
        modelBuilder.UseIdentityByDefaultColumns();

        modelBuilder.Entity<User>(b =>
        {
            b.Property<int>(x => x.UserId).ValueGeneratedOnAdd();
            b.Property<string>(x => x.DisplayName).IsRequired().HasMaxLength(100);
            b.Property<string>(x => x.NormalizedDisplayName).IsRequired().HasMaxLength(100);
            b.Property<string>(x => x.RealName).HasMaxLength(200);
            b.Property<string>(x => x.Email).HasMaxLength(320);
            b.Property<string>(x => x.NormalizedEmail).HasMaxLength(320);
            b.Property<bool>(x => x.IsAnonymous);
            b.Property<string>(x => x.PasswordHash);
            b.HasKey(x => x.UserId);
            b.HasIndex(x => x.NormalizedDisplayName).IsUnique();
            b.HasIndex(x => x.NormalizedEmail).IsUnique().HasFilter("\"NormalizedEmail\" IS NOT NULL AND \"NormalizedEmail\" <> ''");
        });

        modelBuilder.Entity<Game>(b =>
        {
            b.Property<int>(x => x.GameId).ValueGeneratedOnAdd();
            b.Property<string>(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property<DateTime>(x => x.CreatedAt);
            b.Property<bool>(x => x.IsDrawn);
            b.Property<int>(x => x.CreatorId);
            b.Property<int>(x => x.GiftCost);
            b.HasKey(x => x.GameId);
            b.HasIndex(x => x.CreatorId);
            b.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserGame>(b =>
        {
            b.Property<int>(x => x.UserId);
            b.Property<int>(x => x.GameId);
            b.HasKey(x => new { x.UserId, x.GameId });
            b.HasIndex(x => x.GameId);
            b.HasOne(x => x.User).WithMany(x => x.UserGames).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Game).WithMany(x => x.UserGames).HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Wish>(b =>
        {
            b.Property<int>(x => x.WishId).ValueGeneratedOnAdd();
            b.Property<int>(x => x.UserId);
            b.Property<string>(x => x.WishText).IsRequired().HasMaxLength(1000);
            b.Property<DateTime>(x => x.CreatedAt);
            b.HasKey(x => x.WishId);
            b.HasIndex(x => x.UserId);
            b.HasOne(x => x.User).WithMany(x => x.Wishes).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnonGuestCode>(b =>
        {
            b.Property<int>(x => x.CodeId).ValueGeneratedOnAdd();
            b.Property<int>(x => x.UserId);
            b.Property<string>(x => x.AccessCode).IsRequired().HasMaxLength(100);
            b.Property<DateTime?>(x => x.ExpiresAt);
            b.HasKey(x => x.CodeId);
            b.HasIndex(x => x.AccessCode).IsUnique();
            b.HasIndex(x => x.UserId);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeliveryPoint>(b =>
        {
            b.Property<int>(x => x.DeliveryPointId).ValueGeneratedOnAdd();
            b.Property<string>(x => x.Code).IsRequired().HasMaxLength(100);
            b.Property<string>(x => x.Description).HasMaxLength(500);
            b.HasKey(x => x.DeliveryPointId);
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<GameInvite>(b =>
        {
            b.Property<int>(x => x.GameInviteId).ValueGeneratedOnAdd();
            b.Property<int>(x => x.GameId);
            b.Property<string>(x => x.Token).IsRequired().HasMaxLength(100);
            b.Property<DateTime>(x => x.CreatedAt);
            b.HasKey(x => x.GameInviteId);
            b.HasIndex(x => x.GameId).IsUnique();
            b.HasIndex(x => x.Token).IsUnique();
            b.HasOne(x => x.Game).WithMany(x => x.Invites).HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserGift>(b =>
        {
            b.Property<int>(x => x.UserGiftId).ValueGeneratedOnAdd();
            b.Property<int>(x => x.GameId);
            b.Property<int>(x => x.UserId);
            b.Property<string>(x => x.GiftName).IsRequired().HasMaxLength(300);
            b.Property<string>(x => x.DeliveryMethod).IsRequired().HasMaxLength(300);
            b.Property<DateTime>(x => x.CreatedAt);
            b.HasKey(x => x.UserGiftId);
            b.HasIndex(x => x.GameId);
            b.HasIndex(x => x.UserId);
            b.HasOne(x => x.Game).WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SantaAssignment>(b =>
        {
            b.Property<int>(x => x.AssignmentId).ValueGeneratedOnAdd();
            b.Property<int>(x => x.GiverUserId);
            b.Property<int>(x => x.ReceiverUserId);
            b.Property<int>(x => x.GameId);
            b.Property<int?>(x => x.SelectedGiftId);
            b.Property<DateTime>(x => x.CreatedAt);
            b.HasKey(x => x.AssignmentId);
            b.HasIndex(x => new { x.GameId, x.GiverUserId }).IsUnique();
            b.HasIndex(x => new { x.GameId, x.ReceiverUserId }).IsUnique();
            b.HasIndex(x => x.GiverUserId);
            b.HasIndex(x => x.ReceiverUserId);
            b.HasIndex(x => x.SelectedGiftId);
            b.HasOne(x => x.Game).WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Giver).WithMany().HasForeignKey(x => x.GiverUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Receiver).WithMany().HasForeignKey(x => x.ReceiverUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SelectedGift).WithMany().HasForeignKey(x => x.SelectedGiftId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
