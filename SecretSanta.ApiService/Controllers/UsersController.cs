using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SecretSanta.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;

        // Регистрация пользователя
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            user.IsAnonymous = user.IsAnonymous; // можно менять по желанию
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok(user);
        }

        // Присоединиться к игре
        [HttpPost("{userId}/join/{gameId}")]
        public async Task<IActionResult> JoinGame(int userId, int gameId)
        {
            var exists = await _db.UserGames.AnyAsync(ug => ug.UserId == userId && ug.GameId == gameId);
            if (exists) return BadRequest("Участник уже в игре");

            _db.UserGames.Add(new UserGame { UserId = userId, GameId = gameId });
            await _db.SaveChangesAsync();
            return Ok();
        }

        // Список игр для пользователя
        [HttpGet("{userId}/games")]
        public async Task<IActionResult> GetUserGames(int userId)
        {
            var games = await _db.UserGames
                .Where(ug => ug.UserId == userId)
                .Include(ug => ug.Game)
                .Select(ug => ug.Game)
                .ToListAsync();

            return Ok(games);
        }
    }
}