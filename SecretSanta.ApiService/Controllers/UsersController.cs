using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SecretSanta.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController(AppDbContext db) : ControllerBase
    {
        private readonly AppDbContext _db = db;

        // Список игр для пользователя
        [HttpGet("games")]
        public async Task<IActionResult> GetUserGames()
        {
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            var games = await _db.UserGames
                .Where(ug => ug.UserId == userId)
                .Include(ug => ug.Game)
                .Select(ug => ug.Game)
                .ToListAsync();

            return Ok(games);
        }
    }
}
