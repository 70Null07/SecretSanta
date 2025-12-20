using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SecretSanta.ApiService.Controllers
{
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
    public class CreateGameRequest
    {
        public string Name { get; set; } = null!;
        public int GiftCost { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Требуется авторизация
    public class GamesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public GamesController(AppDbContext db) => _db = db;

        // Создать игру
        [HttpPost]
        public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest req, [FromQuery] int creatorUserId)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Name is required");

            var game = new Game { Name = req.Name, CreatorId = creatorUserId, GiftCost = req.GiftCost };
            _db.Games.Add(game);
            await _db.SaveChangesAsync();

            _db.UserGames.Add(new UserGame { GameId = game.GameId, UserId = creatorUserId });
            await _db.SaveChangesAsync();

            var dto = new GameDto
            {
                GameId = game.GameId,
                Name = game.Name,
                CreatedAt = game.CreatedAt,
                IsDrawn = game.IsDrawn,
                ParticipantCount = 1,
                GiftCost = game.GiftCost
            };

            return Ok(dto);
        }


        // Получить все игры (для админов или общего списка)
        [HttpGet]
        public async Task<IActionResult> GetGames()
        {
            var games = await _db.Games.Include(g => g.UserGames).ThenInclude(ug => ug.User).ToListAsync();

            var dto = games.Select(g => new GameListDto
            {
                GameId = g.GameId,
                Name = g.Name,
                CreatedAt = g.CreatedAt,
                IsDrawn = g.IsDrawn,
                Participants = g.UserGames.Select(ug => ug.User.DisplayName).ToList(),
                GiftCost = g.GiftCost
            }).ToList();

            return Ok(dto);
        }

        // Получить игры конкретного пользователя
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserGames(int userId)
        {
            var games = await _db.UserGames
                .Where(ug => ug.UserId == userId)
                .Include(ug => ug.Game)
                .ThenInclude(g => g.UserGames)
                .ThenInclude(ug => ug.User)
                .Select(ug => ug.Game)
                .ToListAsync();

            var dto = games.Select(g => new GameListDto
            {
                GameId = g.GameId,
                Name = g.Name,
                CreatedAt = g.CreatedAt,
                IsDrawn = g.IsDrawn,
                Participants = g.UserGames.Select(ug => ug.User.DisplayName).ToList(),
                CreatorId = g.CreatorId,
                GiftCost = g.GiftCost
            }).ToList();

            return Ok(dto);
        }

        [HttpPost("join/{inviteToken}")]
        public async Task<IActionResult> JoinGame(string inviteToken)
        {
            // Получаем ID пользователя из токена (JWT)
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            // Ищем приглашение по токену
            var invite = await _db.GameInvites
                .Include(i => i.Game)
                .ThenInclude(g => g.UserGames)
                .FirstOrDefaultAsync(i => i.Token == inviteToken);

            if (invite == null)
                return NotFound("Приглашение не найдено");

            // Проверяем, что пользователь ещё не участвует
            bool alreadyJoined = invite.Game.UserGames.Any(ug => ug.UserId == userId);
            if (!alreadyJoined)
            {
                _db.UserGames.Add(new UserGame
                {
                    GameId = invite.Game.GameId,
                    UserId = userId
                });
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost("{gameId}/invite")]
        public async Task<IActionResult> CreateInvite(int gameId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            var game = await _db.Games.FindAsync(gameId);
            if (game == null) return NotFound();

            if (game.CreatorId != userId)
                return Forbid();

            var invite = _db.GameInvites.FirstOrDefault(i => i.GameId == gameId);

            if (invite == null)
            {
                invite = new GameInvite
                {
                    GameId = gameId,
                    Token = Guid.NewGuid().ToString()
                };

                _db.GameInvites.Add(invite);
                await _db.SaveChangesAsync();
            }

            // Возвращаем только токен, фронтенд сам строит ссылку
            return Ok(new { InviteToken = invite.Token });
        }
    }
}
