using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SecretSanta.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssignmentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AssignmentsController(AppDbContext db) => _db = db;
        public class UserGiftDto
        {
            public int UserGiftId { get; set; }
            public string GiftName { get; set; } = null!;
            public string DeliveryMethod { get; set; } = null!;

            public UserGiftDto(string iGiftName, string iDeliv)
            {
                GiftName = iGiftName;
                DeliveryMethod = iDeliv;
            }
        }

        public class RecieverGiftDto
        {
            public string RecieverName { get; set; }
            public List<UserGiftDto> RecieverGifts { get; set; }

            public RecieverGiftDto(string recieverName, List<UserGiftDto> recieverGifts)
            {
                RecieverName = recieverName;
                RecieverGifts = recieverGifts;
            }
        }

        // Жеребьёвка для игры
        [HttpPost("draw/{gameId}")]
        public async Task<IActionResult> Draw(int gameId)
        {
            var game = await _db.Games.Include(g => g.UserGames).ThenInclude(ug => ug.User)
                                      .FirstOrDefaultAsync(g => g.GameId == gameId);
            if (game == null) return NotFound("Игра не найдена");


            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

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
        [HttpGet("giver/{gameId}/{giverId}")]
        public async Task<IActionResult> GetAssignment(int gameId, int giverId)
        {
            var assignment = await _db.SantaAssignments
                .Include(a => a.Receiver)
                .Include(a => a.SelectedGift)
                .FirstOrDefaultAsync(a => a.GiverUserId == giverId && a.GameId == gameId);

            var assigmentWishes = await _db.SantaAssignments
                .Include(a => a.Receiver)
                .Include(a => a.SelectedGift)
                .Where(a => a.GiverUserId == giverId && a.GameId == gameId)
                .ToListAsync();

            var wishes = await _db.UserGifts.Where(a => a.GameId == gameId && a.UserId == assignment.ReceiverUserId).ToListAsync();

            var uname = await _db.Users.Where(a => a.UserId == assignment.ReceiverUserId).FirstOrDefaultAsync();

            List<UserGiftDto> wisheslist = new();

            foreach(var w in wishes)
            {
                wisheslist.Add(new UserGiftDto(w.GiftName, w.DeliveryMethod));
            }

            var recievergifts = new RecieverGiftDto(uname.DisplayName, wisheslist);

            if (assignment == null)
                return NotFound();

            return Ok(recievergifts);
        }
    }
}
