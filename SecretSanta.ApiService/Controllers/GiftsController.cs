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
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            var game = await _db.Games.FindAsync(gameId);
            if (game == null) return NotFound();

            if (!await CanAccessGame(gameId, userId))
                return Forbid();

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
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            if (!await _db.Games.AnyAsync(g => g.GameId == gameId))
                return NotFound("Игра не найдена");

            if (!await CanAccessGame(gameId, userId))
                return Forbid();

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
            if (!this.TryGetCurrentUserId(out var userId))
                return Unauthorized();

            if (!await _db.Games.AnyAsync(g => g.GameId == gameId))
                return NotFound("Игра не найдена");

            if (!await CanAccessGame(gameId, userId))
                return Forbid();

            var gift = await _db.UserGifts.FirstOrDefaultAsync(g =>
                g.UserGiftId == giftId &&
                g.GameId == gameId &&
                g.UserId == userId);

            if (gift == null)
                return NotFound("Подарок не найден");

            _db.UserGifts.Remove(gift);
            await _db.SaveChangesAsync();

            return Ok();
        }

        private Task<bool> CanAccessGame(int gameId, int userId) =>
            _db.Games.AnyAsync(g => g.GameId == gameId &&
                (g.CreatorId == userId || g.UserGames.Any(ug => ug.UserId == userId)));

    }
}
