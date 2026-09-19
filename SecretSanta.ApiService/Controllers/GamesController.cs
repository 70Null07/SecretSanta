using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SecretSanta.ApiService.DTOs;

namespace SecretSanta.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GamesController(AppDbContext db) : ControllerBase
    {
        private readonly AppDbContext _db = db;

        // Создать игру
        [HttpPost]
        public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest req)
        {
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Name))
                return this.ApiError(StatusCodes.Status400BadRequest, "name_required");

            var game = new Game { Name = req.Name, CreatorId = userId, GiftCost = req.GiftCost };
            _db.Games.Add(game);
            await _db.SaveChangesAsync();

            _db.UserGames.Add(new UserGame { GameId = game.GameId, UserId = userId });
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
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            var games = await _db.Games
                .Where(g => g.CreatorId == userId || g.UserGames.Any(ug => ug.UserId == userId))
                .Include(g => g.UserGames).ThenInclude(ug => ug.User).ToListAsync();

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
        [HttpGet("user")]
        public async Task<IActionResult> GetUserGames()
        {
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

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
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            // Ищем приглашение по токену
            var invite = await _db.GameInvites
                .Include(i => i.Game)
                .ThenInclude(g => g.UserGames)
                .FirstOrDefaultAsync(i => i.Token == inviteToken);

            if (invite == null)
                return this.ApiError(StatusCodes.Status404NotFound, "invite_not_found");

            // A completed draw is immutable: adding a participant afterwards would
            // create a member without an assignment and expose a misleading game state.
            if (invite.Game.IsDrawn)
                return this.ApiError(StatusCodes.Status409Conflict, "game_already_drawn");

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
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            var game = await _db.Games.FindAsync(gameId);
            if (game == null) return this.ApiError(StatusCodes.Status404NotFound, "game_not_found");

            if (game.CreatorId != userId)
                return Forbid();

            var invite = await _db.GameInvites.FirstOrDefaultAsync(i => i.GameId == gameId);

            if (invite == null)
            {
                invite = new GameInvite
                {
                    GameId = gameId,
                    Token = Guid.NewGuid().ToString()
                };

                _db.GameInvites.Add(invite);
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException exception) when (IsUniqueViolation(exception))
                {
                    // Another request can create the game's invite first. Detach the failed
                    // insert and return the winner without leaking database diagnostics.
                    _db.Entry(invite).State = EntityState.Detached;
                    invite = await _db.GameInvites.FirstOrDefaultAsync(i => i.GameId == gameId);
                    if (invite == null)
                        return this.ApiError(StatusCodes.Status409Conflict, "invite_creation_failed");
                }
            }

            return Ok(new { InviteToken = invite.Token });
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
