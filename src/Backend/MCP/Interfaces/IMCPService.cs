using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Persistence.Models;

namespace Backend.MCP.Interfaces
{
    /// <summary>
    /// Servicio que coordina los enrutadores MCP (Rule y LLM).
    /// </summary>
    public interface IMCPService
    {
        Task<MCPResult> ProcessQueryAsync(string query);
        List<string> GetAvailableRules();
    }

    /// <summary>
    /// Resultado de una consulta MCP.
    /// </summary>
    public class MCPResult
    {
        public string Response { get; set; } = string.Empty;
        public string RouterUsed { get; set; } = string.Empty; // "Rule" o "LLM"
        public int ResultCount { get; set; }
        public long ExecutionTimeMs { get; set; }
        public List<Card>? Data { get; set; }
    }
}
