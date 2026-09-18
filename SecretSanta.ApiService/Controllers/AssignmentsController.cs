using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            var game = await _db.Games.Include(g => g.UserGames).ThenInclude(ug => ug.User)
                                      .FirstOrDefaultAsync(g => g.GameId == gameId);
            if (game == null) return NotFound("Игра не найдена");


            // Проверяем создателя
            if (game.CreatorId != userId) return Forbid();

            var participants = game.UserGames.Select(ug => ug.User).ToList();
            if (participants.Count < 2) return BadRequest("Недостаточно участников");

            var shuffled = participants.OrderBy(x => Guid.NewGuid()).ToList();

            var rnd = new Random();

            for (int i = 0; i < shuffled.Count; i++)
            {
                var giver = shuffled[i];
                var receiver = shuffled[(i + 1) % shuffled.Count];

                var gifts = await _db.UserGifts
                    .Where(g => g.UserId == receiver.UserId && g.GameId == gameId)
                    .ToListAsync();

                UserGift? selectedGift = null;

                if (gifts.Count > 0)
                {
                    var index = rnd.Next(gifts.Count);
                    selectedGift = gifts[index];
                }

                if(selectedGift == null)
                {
                    _db.SantaAssignments.Add(new SantaAssignment
                    {
                        GameId = gameId,
                        GiverUserId = giver.UserId,
                        ReceiverUserId = receiver.UserId,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
                else 
                    _db.SantaAssignments.Add(new SantaAssignment
                    {
                        GameId = gameId,
                        GiverUserId = giver.UserId,
                        ReceiverUserId = receiver.UserId,
                        CreatedAt = DateTime.UtcNow,
                        SelectedGiftId = selectedGift.UserGiftId
                    });
            }

            game.IsDrawn = true;
            await _db.SaveChangesAsync();
            return Ok("Жеребьёвка выполнена");
        }

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
                .Include(a => a.Receiver)
                .Include(a => a.SelectedGift)
                .FirstOrDefaultAsync(a => a.GiverUserId == userId && a.GameId == gameId);

            if (assignment == null)
                return NotFound();

            var wishes = await _db.UserGifts.Where(a => a.GameId == gameId && a.UserId == assignment.ReceiverUserId).ToListAsync();

            List<UserGiftDto> wisheslist = new();

            foreach(var w in wishes)
            {
                wisheslist.Add(new UserGiftDto(w.GiftName, w.DeliveryMethod));
            }

            var recievergifts = new RecieverGiftDto(assignment.Receiver!.DisplayName, wisheslist);

            return Ok(recievergifts);
        }
    }
}
