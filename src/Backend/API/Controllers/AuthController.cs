using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProyectoFinal.Backend.API.Auth;
using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal.Backend.API.Controllers
{
    /// <summary>
    /// Controlador de autenticación JWT.
    /// Implementa login, registro y validación de tokens.
    /// NOTA: Usuarios hardcodeados para simplificar el proyecto (sin base de datos de usuarios).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;

        // Usuarios hardcodeados (en producción esto estaría en base de datos)
        private static readonly Dictionary<string, UserCredentials> _users = new()
        {
            { "admin", new UserCredentials { Id = "1", Username = "admin", Password = "admin123", Role = "Admin" } },
            { "user", new UserCredentials { Id = "2", Username = "user", Password = "user123", Role = "User" } },
            { "test", new UserCredentials { Id = "3", Username = "test", Password = "test123", Role = "User" } }
        };

        public AuthController(JwtService jwtService)
        {
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        }

        /// <summary>
        /// Autentica un usuario y devuelve un token JWT.
        /// POST /api/auth/login
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Login([FromBody] LoginRequest request)
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
                if (!_users.TryGetValue(request.Username.ToLower(), out var user) ||
                    user.Password != request.Password)
                {
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                var token = _jwtService.GenerateToken(user.Id, user.Username, user.Role);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Username = user.Username,
                    UserId = user.Id,
                    Role = user.Role,
                    ExpiresIn = 3600
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Authentication failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Registra un nuevo usuario (simulado).
        /// POST /api/auth/register
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult Register([FromBody] RegisterRequest request)
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
                if (_users.ContainsKey(request.Username.ToLower()))
                {
                    return Conflict(new { error = "Username already exists" });
                }

                var userId = Guid.NewGuid().ToString();
                var newUser = new UserCredentials
                {
                    Id = userId,
                    Username = request.Username,
                    Password = request.Password, //TODO HASH
                    Role = "User"
                };

                _users[request.Username.ToLower()] = newUser;

                var token = _jwtService.GenerateToken(userId, request.Username, "User");

                return Ok(new RegisterResponse
                {
                    Message = "User registered successfully",
                    Token = token,
                    Username = request.Username,
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Registration failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Valida un token JWT.
        /// POST /api/auth/validate
        /// </summary>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(ValidateTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

                return Ok(new ValidateTokenResponse
                {
                    Valid = true,
                    UserId = userId ?? string.Empty,
                    Username = username ?? string.Empty,
                    Role = role ?? "User"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Validation failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene información del usuario autenticado (requiere JWT).
        /// GET /api/auth/me
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                if (string.IsNullOrWhiteSpace(token))
                {
                    return Unauthorized(new { error = "No token provided" });
                }

                var userId = _jwtService.GetUserIdFromToken(token);
                var username = _jwtService.GetUsernameFromToken(token);
                var role = _jwtService.GetRoleFromToken(token);

                return Ok(new UserInfoResponse
                {
                    UserId = userId ?? string.Empty,
                    Username = username ?? string.Empty,
                    Role = role ?? "User"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to get user info: {ex.Message}" });
            }
        }
    }

    // ==================== MODELOS ====================

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }
    }

    public class RegisterResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class ValidateTokenRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class ValidateTokenResponse
    {
        public bool Valid { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UserInfoResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    internal class UserCredentials
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }
}
