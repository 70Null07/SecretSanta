using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SecretSanta.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WishesController(AppDbContext db) : ControllerBase
    {
        private readonly AppDbContext _db = db;

        [HttpPost]
        public async Task<IActionResult> AddWish([FromBody] Wish wish)
        {
            wish.CreatedAt = DateTime.UtcNow;
            _db.Wishes.Add(wish);
            await _db.SaveChangesAsync();
            return Ok(wish);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetWishes(int userId)
        {
            var wishes = await _db.Wishes.Where(w => w.UserId == userId).ToListAsync();
            return Ok(wishes);
        }
    }
}
