using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.RateLimiting;
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
        [EnableRateLimiting("authentication")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var normalizedName = LoginIdentifier.Normalize(req.DisplayName);
            var normalizedEmail = LoginIdentifier.NormalizeOptional(req.Email);
            var existsName = await _db.Users.AnyAsync(u => u.NormalizedDisplayName == normalizedName);
            if (existsName) return BadRequest("Имя уже занято");

            if (normalizedEmail != null &&
                await _db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
                return BadRequest("Email уже используется");

            var user = new User
            {
                DisplayName = req.DisplayName,
                NormalizedDisplayName = normalizedName,
                RealName = req.RealName,
                Email = req.Email,
                IsAnonymous = req.IsAnonymous
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, req.Password);

            _db.Users.Add(user);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                // A concurrent registration may win after the checks above. Never expose
                // provider exception details or constraint names to the client.
                return Conflict("Имя или email уже используются");
            }

            var dto = new UserDto { UserId = user.UserId, DisplayName = user.DisplayName, Email = user.Email };
            return Ok(dto);
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

        [HttpPost("login")]
        [EnableRateLimiting("authentication")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var normalizedIdentifier = LoginIdentifier.Normalize(req.Identifier);
            var user = await _db.Users
                .Where(u => u.NormalizedEmail == normalizedIdentifier ||
                            u.NormalizedDisplayName == normalizedIdentifier)
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
