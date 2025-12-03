using System;
using System.Threading.Tasks;
using Backend.MCP.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoFinal.Backend.API.Controllers
{
    /// <summary>
    /// Controlador para el protocolo MCP (Model Context Protocol).
    /// Permite consultas en lenguaje natural sobre las cartas de Magic.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere JWT
    public class MCPController : ControllerBase
    {
        private readonly IMCPService _mcpService;

        public MCPController(IMCPService mcpService)
        {
            _mcpService = mcpService;
        }

        /// <summary>
        /// Procesa una consulta en lenguaje natural.
        /// Primero intenta con RuleRouter, luego con LLMRouter si no hay coincidencias.
        /// POST: api/mcp/query
        /// </summary>
        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] MCPQueryRequest request)
        {
            // Validación de entrada
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
                return BadRequest(new { error = "Query is too long (max 500 characters)" });
            }

            try
            {
                var result = await _mcpService.ProcessQueryAsync(request.Query);

                return Ok(new
                {
                    query = request.Query,
                    response = result.Response,
                    router = result.RouterUsed, // "Rule" o "LLM"
                    resultCount = result.ResultCount,
                    executionTimeMs = result.ExecutionTimeMs,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to process query",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene información sobre las reglas disponibles en el RuleRouter.
        /// GET: api/mcp/rules
        /// </summary>
        [HttpGet("rules")]
        public IActionResult GetAvailableRules()
        {
            try
            {
                var rules = _mcpService.GetAvailableRules();

                return Ok(new
                {
                    count = rules.Count,
                    rules = rules,
                    examples = new[]
                    {
                        "¿Cuántas cartas hay?",
                        "Muéstrame cartas azules",
                        "Busca criaturas rojas",
                        "Cartas del set Zendikar",
                        "Carta llamada Lightning Bolt"
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
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                service = "MCP Service",
                timestamp = DateTime.UtcNow
            });
        }
    }

    // ==================== MODELOS ====================

    /// <summary>
    /// Modelo de request para consultas MCP.
    /// </summary>
    public class MCPQueryRequest
    {
        public string Query { get; set; } = string.Empty;
    }
}
