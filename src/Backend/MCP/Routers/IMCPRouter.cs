using System.Threading.Tasks;

namespace Backend.MCP.Routers
{
    public interface IMCPRouter
    {
        Task<string> ProcessRequestAsync(string query);
        bool CanHandle(string query);
    }
}
