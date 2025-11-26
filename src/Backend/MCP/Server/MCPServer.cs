using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.MCP.Routers;
using Backend.Persistence.Interfaces;
using Backend.API.Models;

namespace Backend.MCP.Server
{
    public class MCPServer
    {
        private readonly List<IMCPRouter> _routers;

        public MCPServer(IRepository<Card> repository)
        {
            _routers = new List<IMCPRouter>
            {
                new RuleRouter(repository),
                new LLMRouter()
            };
        }

        public async Task<string> ProcessQueryAsync(string query)
        {
            foreach (var router in _routers)
            {
                if (router.CanHandle(query))
                {
                    // If it's the RuleRouter, it might return a result or pass if it can't really handle it (though CanHandle checks regex).
                    // For LLM, it's a catch-all.
                    // We can refine this logic.
                    if (router is RuleRouter && !router.CanHandle(query)) continue;
                    
                    return await router.ProcessRequestAsync(query);
                }
            }
            return "No router could handle the request.";
        }
    }
}
