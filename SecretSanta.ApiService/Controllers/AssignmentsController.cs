using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography;

namespace SecretSanta.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssignmentsController(AppDbContext db) : ControllerBase
    {
        private readonly AppDbContext _db = db;

        public class UserGiftDto(string iGiftName, string iDeliv)
        {
            public int UserGiftId { get; set; }
            public string GiftName { get; set; } = iGiftName;
            public string DeliveryMethod { get; set; } = iDeliv;
        }

        public class RecieverGiftDto(string recieverName, List<AssignmentsController.UserGiftDto> recieverGifts)
        {
            public string RecieverName { get; set; } = recieverName;
            public List<UserGiftDto> RecieverGifts { get; set; } = recieverGifts;
        }

        // Жеребьёвка для игры
        [HttpPost("draw/{gameId}")]
        public async Task<IActionResult> Draw(int gameId)
        {
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var game = await _db.Games
                    .Include(g => g.UserGames)
                    .ThenInclude(ug => ug.User)
                    .FirstOrDefaultAsync(g => g.GameId == gameId);
                if (game == null) return NotFound("Игра не найдена");

                // Проверяем создателя
                if (game.CreatorId != userId) return Forbid();
                if (game.IsDrawn) return Conflict("Игра уже разыграна");

                var participants = game.UserGames.Select(ug => ug.User).ToList();
                if (participants.Count < 2) return BadRequest("Недостаточно участников");

                Shuffle(participants);

                var giftsByUser = (await _db.UserGifts
                        .Where(g => g.GameId == gameId)
                        .ToListAsync())
                    .GroupBy(g => g.UserId)
                    .ToDictionary(group => group.Key, group => group.ToList());

                for (var i = 0; i < participants.Count; i++)
                {
                    var giver = participants[i];
                    var receiver = participants[(i + 1) % participants.Count];

                    UserGift? selectedGift = null;
                    if (giftsByUser.TryGetValue(receiver.UserId, out var gifts) && gifts.Count > 0)
                        selectedGift = gifts[RandomNumberGenerator.GetInt32(gifts.Count)];

                    _db.SantaAssignments.Add(new SantaAssignment
                    {
                        GameId = gameId,
                        GiverUserId = giver.UserId,
                        ReceiverUserId = receiver.UserId,
                        CreatedAt = DateTime.UtcNow,
                        SelectedGiftId = selectedGift?.UserGiftId
                    });
                }

                game.IsDrawn = true;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok("Жеребьёвка выполнена");
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                return Conflict("Игра уже разыграна");
            }
        }

        private static void Shuffle<T>(IList<T> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            };

        // Посмотреть кому дарить
        [HttpGet("giver/{gameId}")]
        public async Task<IActionResult> GetAssignment(int gameId)
        {
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            var isParticipant = await _db.UserGames
                .AnyAsync(ug => ug.GameId == gameId && ug.UserId == userId);
            if (!isParticipant)
                return Forbid();

            var assignment = await _db.SantaAssignments
                .FirstOrDefaultAsync(a => a.GiverUserId == userId && a.GameId == gameId);

            if (assignment == null)
                return NotFound();

            var receiver = await _db.Users
                .FirstOrDefaultAsync(user => user.UserId == assignment.ReceiverUserId);
            if (receiver == null)
                return NotFound("Получатель не найден");

            var wishes = await _db.UserGifts.Where(a => a.GameId == gameId && a.UserId == assignment.ReceiverUserId).ToListAsync();

            List<UserGiftDto> wisheslist = new();

            foreach(var w in wishes)
            {
                wisheslist.Add(new UserGiftDto(w.GiftName, w.DeliveryMethod));
            }

            var recievergifts = new RecieverGiftDto(receiver.DisplayName, wisheslist);

            return Ok(recievergifts);
        }
    }
}
