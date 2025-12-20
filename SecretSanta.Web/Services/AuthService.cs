namespace SecretSanta.Web.Services
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Http.Json;

    public class RegisterRequest
    {
        [Required] public string DisplayName { get; set; } = null!;
        public string? RealName { get; set; }
        [EmailAddress] public string? Email { get; set; }

        [Required] public string Password { get; set; } = null!;
        public bool IsAnonymous { get; set; } = false;
    }

    public class LoginRequest
    {
        [Required] public string Identifier { get; set; } = null!;
        // Identifier = email OR displayname (просто пример)
        [Required] public string Password { get; set; } = null!;
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Email { get; set; }
    }


    public class AuthService
    {
        private readonly HttpClient _http;
        public UserDto? CurrentUser { get; private set; }

        public AuthService(HttpClient http)
        {
            _http = http;
        }

        public async Task<bool> Login(string identifier, string password)
        {
            var res = await _http.PostAsJsonAsync("api/auth/login", new
            {
                identifier,
                password
            });

            if (!res.IsSuccessStatusCode)
                return false;

            CurrentUser = await res.Content.ReadFromJsonAsync<UserDto>();
            return true;
        }

        public async Task<bool> Register(string displayName, string? email, string password)
        {
            var res = await _http.PostAsJsonAsync("api/auth/register", new
            {
                displayName,
                email,
                password,
                isAnonymous = false
            });

            if (!res.IsSuccessStatusCode)
                return false;

            CurrentUser = await res.Content.ReadFromJsonAsync<UserDto>();
            return true;
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }

}
