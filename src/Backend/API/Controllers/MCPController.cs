using System.ComponentModel.DataAnnotations;
using Backend.MCP.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoFinal.Backend.API.Controllers
{
    /// <summary>
    /// Controlador para el protocolo MCP (Model Context Protocol).
    /// Permite consultas en lenguaje natural sobre las cartas de Magic: The Gathering.
    /// Implementa enrutamiento dual: RuleRouter (reglas) y LLMRouter (IA).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere JWT
    public class MCPController : ControllerBase
    {
        private readonly IMCPService _mcpService;

        public MCPController(IMCPService mcpService)
        {
            _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
        }

        /// <summary>
        /// Procesa una consulta en lenguaje natural.
        /// Primero intenta con RuleRouter (reglas), luego con LLMRouter (IA) si no hay coincidencias.
        /// POST: api/mcp/query
        /// </summary>
        [HttpPost("query")]
        [ProducesResponseType(typeof(MCPQueryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Query([FromBody] MCPQueryRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query is required" });
            }

            if (request.Query.Length > 500)
            {
                return BadRequest(new { error = "Query is too long (maximum 500 characters)" });
            }

            try
            {
                var result = await _mcpService.ProcessQueryAsync(request.Query);

                return Ok(new MCPQueryResponse
                {
                    Query = request.Query,
                    Response = result.Response,
                    RouterUsed = result.RouterUsed, // "Rule" o "LLM"
                    ResultCount = result.ResultCount,
                    ExecutionTimeMs = result.ExecutionTimeMs,
                    Data = result.Data,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to process MCP query",
                    query = request.Query,
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene información sobre las reglas disponibles en el RuleRouter.
        /// GET: api/mcp/rules
        /// </summary>
        [HttpGet("rules")]
        [ProducesResponseType(typeof(AvailableRulesResponse), StatusCodes.Status200OK)]
        public IActionResult GetAvailableRules()
        {
            try
            {
                var rules = _mcpService.GetAvailableRules();

                return Ok(new AvailableRulesResponse
                {
                    Count = rules.Count,
                    Rules = rules,
                    Examples = new[]
                    {
                        "Cuantas cartas hay?",
                        "Muestrame cartas azules",
                        "Busca criaturas rojas",
                        "Cartas del set Zendikar",
                        "Carta llamada Lightning Bolt",
                        "Cartas raras",
                        "Criaturas con poder mayor a 5"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve rules: {ex.Message}" });
            }
        }

        /// <summary>
        /// Endpoint de prueba para verificar que el servicio MCP está funcionando.
        /// GET: api/mcp/health
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
        public IActionResult HealthCheck()
        {
            try
            {
                var rulesCount = _mcpService.GetAvailableRules().Count;

                return Ok(new HealthCheckResponse
                {
                    Status = "healthy",
                    Service = "MCP Service (Model Context Protocol)",
                    RulesAvailable = rulesCount,
                    LLMEnabled = true, // Verificar si hay API key configurada
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de uso del servicio MCP (opcional).
        /// GET: api/mcp/stats
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(MCPStatsResponse), StatusCodes.Status200OK)]
        public IActionResult GetMCPStats()
        {
            try
            {
                // Aquí podrías implementar un tracking de queries procesadas
                // Por ahora devolvemos info básica
                return Ok(new MCPStatsResponse
                {
                    RulesAvailable = _mcpService.GetAvailableRules().Count,
                    RouterTypes = new[] { "RuleRouter", "LLMRouter" },
                    SupportedLanguages = new[] { "Spanish", "English" }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve stats: {ex.Message}" });
            }
        }
    }

    // ==================== REQUEST MODELS ====================

    public class MCPQueryRequest
    {
        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Query { get; set; } = string.Empty;
    }

    // ==================== RESPONSE MODELS ====================

    public class MCPQueryResponse
    {
        public string Query { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string RouterUsed { get; set; } = string.Empty; // "Rule" o "LLM"
        public int ResultCount { get; set; }
        public long ExecutionTimeMs { get; set; }
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AvailableRulesResponse
    {
        public int Count { get; set; }
        public List<string> Rules { get; set; } = new();
        public string[] Examples { get; set; } = Array.Empty<string>();
    }

    public class HealthCheckResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public int RulesAvailable { get; set; }
        public bool LLMEnabled { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MCPStatsResponse
    {
        public int RulesAvailable { get; set; }
        public string[] RouterTypes { get; set; } = Array.Empty<string>();
        public string[] SupportedLanguages { get; set; } = Array.Empty<string>();
    }
}
