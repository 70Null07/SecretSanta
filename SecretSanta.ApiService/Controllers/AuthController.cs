using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretSanta.ApiService.DTOs;
using SecretSanta.ApiService.Services;

namespace SecretSanta.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(AppDbContext db, IPasswordHasher<User> passwordHasher, JwtService jwt) : ControllerBase
    {
        private readonly AppDbContext _db = db;
        private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
        private readonly JwtService _jwt = jwt;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existsName = await _db.Users.AnyAsync(u => u.DisplayName == req.DisplayName);
            if (existsName) return BadRequest("Имя уже занято");

            var user = new User
            {
                DisplayName = req.DisplayName,
                RealName = req.RealName,
                Email = req.Email,
                IsAnonymous = req.IsAnonymous
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, req.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var dto = new UserDto { UserId = user.UserId, DisplayName = user.DisplayName, Email = user.Email };
            return Ok(dto);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _db.Users
                .Where(u => (u.Email != null && u.Email == req.Identifier) || u.DisplayName == req.Identifier)
                .FirstOrDefaultAsync();

            if (user == null)
                return Unauthorized("Неверный логин или пароль");

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", req.Password);
            if (verify == PasswordVerificationResult.Failed)
                return Unauthorized("Неверный логин или пароль");

            var token = _jwt.GenerateToken(user.UserId, user.DisplayName);

            var dto = new UserDto
            {
                UserId = user.UserId,
                DisplayName = user.DisplayName,
                Email = user.Email
            };

            return Ok(new
            {
                user = dto,
                token
            });
        }

        [HttpGet("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            return Ok(new { Valid = true });
        }
    }
}
