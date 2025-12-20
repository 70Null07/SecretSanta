using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretSanta.ApiService.DTOs;

namespace SecretSanta.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GiftsController(AppDbContext db) : ControllerBase
    {
        private readonly AppDbContext _db = db;

        [HttpPost("{gameId}/gifts")]
        public async Task<IActionResult> AddGift(int gameId, [FromBody] UserGiftDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            var game = await _db.Games.FindAsync(gameId);
            if (game == null) return NotFound();

            var gift = new UserGift
            {
                GameId = gameId,
                UserId = userId,
                GiftName = dto.GiftName,
                DeliveryMethod = dto.DeliveryMethod
            };
            try
            {
                _db.UserGifts.Add(gift);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok(dto);
        }

        [HttpGet("{gameId}/gifts")]
        public async Task<IActionResult> GetUserGifts(int gameId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            var gifts = await _db.UserGifts
                .Where(g => g.GameId == gameId && g.UserId == userId)
                .Select(g => new UserGiftDto
                {
                    UserGiftId = g.UserGiftId,
                    GiftName = g.GiftName,
                    DeliveryMethod = g.DeliveryMethod
                })
                .ToListAsync();

            return Ok(gifts);
        }

        [HttpDelete("{gameId}/gifts/{giftId}")]
        public async Task<IActionResult> DeleteGift(int gameId, int giftId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            var gift = await _db.UserGifts.FirstOrDefaultAsync(g =>
                g.UserGiftId == giftId &&
                g.GameId == gameId &&
                g.UserId == userId);

            if (gift == null)
                return NotFound("Подарок не найден");

            var game = await _db.Games.FindAsync(gameId);
            if (game == null)
                return NotFound("Игра не найдена");

            _db.UserGifts.Remove(gift);
            await _db.SaveChangesAsync();

            return Ok();
        }

    }
}
