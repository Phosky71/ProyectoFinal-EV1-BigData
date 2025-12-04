using Backend.Persistence.Models;
using Backend.Persistence.Interfaces;

namespace Backend.MCP.Interfaces
{
    /// <summary>
    /// Interfaz para el enrutador de reglas manuales.
    /// </summary>
    public interface IRuleRouter
    {
        Task<RouterResult> ProcessAsync(string query);
        List<string> GetAvailableRules();
    }

    /// <summary>
    /// Interfaz para el enrutador con LLM.
    /// </summary>
    public interface ILLMRouter
    {
        Task<RouterResult> ProcessAsync(string query, IRepository<Card> repository);
    }

    /// <summary>
    /// Resultado de un router (Rule o LLM).
    /// </summary>
    public class RouterResult
    {
        public bool Success { get; set; }
        public string Response { get; set; } = string.Empty;
        public List<Card>? Data { get; set; }
    }
}
