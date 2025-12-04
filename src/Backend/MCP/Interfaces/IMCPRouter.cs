using System.Threading.Tasks;

namespace Backend.MCP.Interfaces
{
    /// <summary>
    /// Router MCP: procesa una consulta si la puede manejar.
    /// </summary>
    public interface IMCPRouter
    {
        bool CanHandle(string query);
        Task<string> ProcessRequestAsync(string query);
    }
}
