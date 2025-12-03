using Microsoft.AspNetCore.Mvc;
using ProyectoFinal.Backend.API.Auth;
using System.Threading.Tasks;

namespace ProyectoFinal.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;

        public AuthController(JwtService jwtService)
        {
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { error = "Username is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Password is required" });
            }

            try
            {
                var isValid = await ValidateCredentialsAsync(request.Username, request.Password);

                if (!isValid)
                {
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                var userId = GetUserIdByUsername(request.Username);
                var role = GetUserRole(request.Username);

                var token = _jwtService.GenerateToken(userId, request.Username, role);

                return Ok(new
                {
                    token = token,
                    username = request.Username,
                    role = role,
                    expiresIn = 3600
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Authentication failed: {ex.Message}" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            {
                return BadRequest(new { error = "Username must be at least 3 characters" });
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return BadRequest(new { error = "Password must be at least 6 characters" });
            }

            try
            {
                if (await UserExistsAsync(request.Username))
                {
                    return Conflict(new { error = "Username already exists" });
                }

                var userId = Guid.NewGuid().ToString();
                var token = _jwtService.GenerateToken(userId, request.Username, "User");

                return Ok(new
                {
                    message = "User registered successfully",
                    token = token,
                    username = request.Username
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Registration failed: {ex.Message}" });
            }
        }

        [HttpPost("validate")]
        public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { error = "Token is required" });
            }

            try
            {
                var principal = _jwtService.ValidateToken(request.Token);

                if (principal == null)
                {
                    return Unauthorized(new { valid = false, error = "Invalid or expired token" });
                }

                var userId = _jwtService.GetUserIdFromToken(request.Token);
                var username = _jwtService.GetUsernameFromToken(request.Token);
                var role = _jwtService.GetRoleFromToken(request.Token);

                return Ok(new
                {
                    valid = true,
                    userId = userId,
                    username = username,
                    role = role
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Validation failed: {ex.Message}" });
            }
        }

        private async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            await Task.Delay(100);

            var validUsers = new Dictionary<string, string>
            {
                { "admin", "admin123" },
                { "user", "user123" },
                { "test", "test123" }
            };

            return validUsers.TryGetValue(username, out var validPassword) &&
                   validPassword == password;
        }

        private string GetUserIdByUsername(string username)
        {
            var userIds = new Dictionary<string, string>
            {
                { "admin", "1" },
                { "user", "2" },
                { "test", "3" }
            };

            return userIds.TryGetValue(username, out var id) ? id : Guid.NewGuid().ToString();
        }

        private string GetUserRole(string username)
        {
            return username.ToLower() == "admin" ? "Admin" : "User";
        }

        private async Task<bool> UserExistsAsync(string username)
        {
            await Task.Delay(50);
            var existingUsers = new[] { "admin", "user", "test" };
            return existingUsers.Contains(username.ToLower());
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ValidateTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
