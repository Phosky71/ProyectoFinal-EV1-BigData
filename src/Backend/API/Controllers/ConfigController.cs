using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoFinal.Backend.API.Services;

namespace ProyectoFinal.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere JWT para acceder
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationService _configService;

        public ConfigController(IConfiguration configuration, IConfigurationService configService)
        {
            _configuration = configuration;
            _configService = configService;
        }

        /// <summary>
        /// Obtiene la configuración actual del sistema de persistencia.
        /// GET: api/config/persistence
        /// </summary>
        [HttpGet("persistence")]
        public async Task<IActionResult> GetPersistenceMode()
        {
            try
            {
                var currentMode = await _configService.GetPersistenceModeAsync();
                var connectionString = _configuration.GetConnectionString("MySQL");

                return Ok(new
                {
                    persistenceMode = currentMode,
                    availableModes = new[] { "Memory", "MySQL" },
                    hasConnectionString = !string.IsNullOrEmpty(connectionString)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error retrieving configuration: {ex.Message}" });
            }
        }

        /// <summary>
        /// Cambia el sistema de persistencia en runtime.
        /// POST: api/config/persistence
        /// </summary>
        [HttpPost("persistence")]
        public async Task<IActionResult> SetPersistenceMode([FromBody] PersistenceModeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mode))
            {
                return BadRequest(new { error = "Persistence mode is required" });
            }

            // Validar que el modo sea válido
            var validModes = new[] { "Memory", "MySQL" };
            if (!validModes.Contains(request.Mode, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    error = $"Invalid persistence mode: {request.Mode}",
                    validModes = validModes
                });
            }

            try
            {
                await _configService.SetPersistenceModeAsync(request.Mode);

                return Ok(new
                {
                    message = $"Persistence mode switched to {request.Mode}",
                    newMode = request.Mode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error changing persistence mode: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene toda la configuración del sistema.
        /// GET: api/config
        /// </summary>
        [HttpGet]
        public IActionResult GetConfiguration()
        {
            try
            {
                var config = new
                {
                    jwt = new
                    {
                        issuer = _configuration["Jwt:Issuer"],
                        audience = _configuration["Jwt:Audience"],
                        expirationMinutes = _configuration["Jwt:ExpirationMinutes"]
                    },
                    persistence = new
                    {
                        mode = _configuration["Persistence:Mode"],
                        hasMySQL = !string.IsNullOrEmpty(_configuration.GetConnectionString("MySQL"))
                    },
                    kaggle = new
                    {
                        datasetPath = _configuration["Kaggle:DatasetPath"]
                    }
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error retrieving configuration: {ex.Message}" });
            }
        }
    }

    /// <summary>
    /// Modelo para cambiar el modo de persistencia.
    /// </summary>
    public class PersistenceModeRequest
    {
        public string Mode { get; set; } = string.Empty; // "Memory" o "MySQL"
    }
}
