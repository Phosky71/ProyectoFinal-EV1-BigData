// MCPServer.cs - Servidor del protocolo MCP
// Procesa consultas en lenguaje natural sobre la base de datos

namespace Backend.MCP.Server
{
    /// <summary>
    /// Servidor MCP para consultas en lenguaje natural
    /// Utiliza los routers para procesar las peticiones
    /// </summary>
    public class MCPServer
    {
        private readonly RuleRouter _ruleRouter;
        private readonly LLMRouter _llmRouter;
        
        public MCPServer(RuleRouter ruleRouter, LLMRouter llmRouter)
        {
            _ruleRouter = ruleRouter;
            _llmRouter = llmRouter;
        }
        
        /// <summary>
        /// Procesa una consulta en lenguaje natural
        /// 1. Primero intenta con el enrutador de reglas
        /// 2. Si no encuentra regla, usa el LLM
        /// </summary>
        public async Task<string> ProcessQueryAsync(string naturalLanguageQuery)
        {
            // Intentar primero con reglas manuales
            var result = await _ruleRouter.TryRouteAsync(naturalLanguageQuery);
            
            if (result != null)
                return result;
            
            // Si no hay regla, usar LLM
            return await _llmRouter.RouteAsync(naturalLanguageQuery);
        }
    }
}
