using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretSanta.ApiService.DTOs;

namespace SecretSanta.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishesController(AppDbContext db) : ControllerBase
    {
        private readonly AppDbContext _db = db;

        [HttpPost]
        public async Task<IActionResult> AddWish([FromBody] CreateWishRequest request)
        {
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            var wish = new Wish
            {
                UserId = userId,
                WishText = request.WishText,
                CreatedAt = DateTime.UtcNow
            };
            _db.Wishes.Add(wish);
            await _db.SaveChangesAsync();
            return Ok(wish);
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetWishes()
        {
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            var wishes = await _db.Wishes.Where(w => w.UserId == userId).ToListAsync();
            return Ok(wishes);
        }
    }
}
