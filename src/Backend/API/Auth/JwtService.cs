using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ProyectoFinal.Backend.API.Auth
{
    /// <summary>
    /// Servicio para generación y validación de tokens JWT.
    /// </summary>
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

        public JwtService(string secretKey, string issuer, string audience, int expirationMinutes = 60)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new ArgumentException("Secret key cannot be null or empty", nameof(secretKey));
            if (string.IsNullOrWhiteSpace(issuer))
                throw new ArgumentException("Issuer cannot be null or empty", nameof(issuer));
            if (string.IsNullOrWhiteSpace(audience))
                throw new ArgumentException("Audience cannot be null or empty", nameof(audience));

            _secretKey = secretKey;
            _issuer = issuer;
            _audience = audience;
            _expirationMinutes = expirationMinutes;
        }

        /// <summary>
        /// Genera un token JWT para el usuario.
        /// </summary>
        public string GenerateToken(string userId, string username, string role = "User")
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId), // FIX: Necesario para GetUserIdFromToken
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Valida un token JWT.
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // No tolerancia de tiempo
                }, out _);

                return principal;
            }
            catch (Exception)
            {
                // Token inválido, expirado o manipulado
                return null;
            }
        }

        /// <summary>
        /// Extrae el ID de usuario del token.
        /// </summary>
        public string? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Extrae el nombre de usuario del token.
        /// </summary>
        public string? GetUsernameFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst(ClaimTypes.Name)?.Value;
        }

        /// <summary>
        /// Extrae el rol del usuario del token.
        /// </summary>
        public string? GetRoleFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}
