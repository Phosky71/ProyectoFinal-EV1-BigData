using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Persistence.Models;

namespace Backend.MCP.Interfaces
{
    /// <summary>
    /// Servicio que coordina los enrutadores MCP (Model Context Protocol).
    /// Implementa cascada: RuleRouter (reglas manuales) -> LLMRouter (IA).
    /// </summary>
    public interface IMCPService
    {
        /// <summary>
        /// Procesa una consulta en lenguaje natural.
        /// Intenta primero con RuleRouter, si no hay coincidencia usa LLMRouter.
        /// </summary>
        Task<MCPResult> ProcessQueryAsync(string query);

        /// <summary>
        /// Obtiene la lista de reglas disponibles en el RuleRouter.
        /// </summary>
        List<string> GetAvailableRules();
    }

    /// <summary>
    /// Resultado de una consulta MCP.
    /// </summary>
    public class MCPResult
    {
        /// <summary>
        /// Respuesta en lenguaje natural generada por el router.
        /// </summary>
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// Router que procesó la consulta: "Rule", "LLM" o "Error".
        /// </summary>
        public string RouterUsed { get; set; } = string.Empty;

        /// <summary>
        /// Número de cartas devueltas en la respuesta.
        /// </summary>
        public int ResultCount { get; set; }

        /// <summary>
        /// Tiempo de ejecución en milisegundos.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Datos de las cartas (puede ser null si es una consulta informativa).
        /// </summary>
        public List<Card>? Data { get; set; }

        /// <summary>
        /// Indica si la consulta fue exitosa.
        /// </summary>
        public bool Success => RouterUsed != "Error";
    }
}
