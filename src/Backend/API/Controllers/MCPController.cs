using Backend.MCP.Client;
using Backend.MCP.Protocol;
using Backend.MCP.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ProyectoFinal.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MCPController : ControllerBase
    {
        private readonly McpClientService _mcpClient;
        private readonly MagicCardMcpServer _mcpServer;

        public MCPController(McpClientService mcpClient, MagicCardMcpServer mcpServer)
        {
            _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
            _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
        }

        [HttpPost("query")]
        [ProducesResponseType(typeof(MCPQueryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Query([FromBody] MCPQueryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { error = "Query is required" });

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var (resultText, resultData) = await _mcpClient.ProcessUserQueryAsync(request.Query);

                stopwatch.Stop();

                int count = 0;
                if (resultData is System.Collections.ICollection collection) count = collection.Count;


                return Ok(new MCPQueryResponse
                {
                    Query = request.Query,
                    Response = resultText,
                    RouterUsed = "MCP-Agent (Local LLM)",
                    ResultCount = count,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Data = resultData,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to process MCP query", details = ex.Message });
            }
        }

        [HttpGet("rules")]
        public async Task<IActionResult> GetAvailableTools()
        {
            try
            {
                var request = new JsonRpcRequest { Method = "tools/list", Id = "internal" };
                var response = await _mcpServer.HandleRequestAsync(request);
                var toolList = JsonSerializer.Deserialize<ToolListResult>(JsonSerializer.Serialize(response.Result));

                var toolNames = toolList?.Tools.Select(t => $"{t.Name}: {t.Description}").ToList() ?? new List<string>();

                return Ok(new AvailableRulesResponse
                {
                    Count = toolNames.Count,
                    Rules = toolNames,
                    Examples = new[] { "Busca Black Lotus", "Estadísticas de colección" }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult HealthCheck() => Ok(new HealthCheckResponse { Status = "healthy", Service = "MCP Standard", LLMEnabled = true, Timestamp = DateTime.UtcNow });

        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetMCPStats() => Ok(new MCPStatsResponse { RulesAvailable = 2, RouterTypes = new[] { "McpClient", "McpServer" }, SupportedLanguages = new[] { "Spanish" } });
    }

    public class MCPQueryRequest { [Required] public string Query { get; set; } = string.Empty; }
    public class MCPQueryResponse
    {
        public string Query { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string RouterUsed { get; set; } = string.Empty;
        public int ResultCount { get; set; }
        public long ExecutionTimeMs { get; set; }
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public class AvailableRulesResponse { public int Count { get; set; } public List<string> Rules { get; set; } = new(); public string[] Examples { get; set; } = Array.Empty<string>(); }
    public class HealthCheckResponse { public string Status { get; set; } = ""; public string Service { get; set; } = ""; public int RulesAvailable { get; set; } public bool LLMEnabled { get; set; } public DateTime Timestamp { get; set; } }
    public class MCPStatsResponse { public int RulesAvailable { get; set; } public string[] RouterTypes { get; set; } = Array.Empty<string>(); public string[] SupportedLanguages { get; set; } = Array.Empty<string>(); }
}