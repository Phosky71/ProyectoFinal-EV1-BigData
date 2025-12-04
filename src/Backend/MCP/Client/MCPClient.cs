using Backend.MCP.Server;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;

namespace Backend.MCP.Client
{
    /// <summary>
    /// Cliente MCP que coordina las consultas entre los diferentes routers.
    /// Implementa la logica de enrutamiento: RuleRouter primero, LLMRouter como fallback.
    /// </summary>
    public class MCPClient
    {
        private readonly RuleRouter _ruleRouter;
        private readonly LLMRouter _llmRouter;
        private readonly MCPServer _server;

        public MCPClient(IRepository<Card> repository)
        {
            _ruleRouter = new RuleRouter(repository);
            _llmRouter = new LLMRouter();
            _server = new MCPServer(repository);
        }

        /// <summary>
        /// Procesa una consulta en lenguaje natural.
        /// Primero intenta con RuleRouter, si no encuentra regla usa LLMRouter.
        /// </summary>
        /// <param name="query">Consulta en lenguaje natural</param>
        /// <returns>Respuesta procesada</returns>
        public async Task<string> ProcessQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Error: La consulta no puede estar vacia.";
            }

            try
            {
                // Paso 1: Intentar con RuleRouter (reglas manuales)
                // RuleRouter.CanHandle checks if it matches regex
                if (_ruleRouter.CanHandle(query))
                {
                    LogQuery(query, "RuleRouter", true);
                    return await _ruleRouter.ProcessRequestAsync(query);
                }

                // Paso 2: Fallback a LLMRouter
                LogQuery(query, "LLMRouter", false);
                // LLMRouter handles everything
                return await _llmRouter.ProcessRequestAsync(query);
            }
            catch (Exception ex)
            {
                return $"Error al procesar la consulta: {ex.Message}";
            }
        }

        /// <summary>
        /// Verifica si la consulta puede ser manejada por alguna regla manual.
        /// </summary>
        public bool HasMatchingRule(string query)
        {
            return _ruleRouter.CanHandle(query);
        }

        /// <summary>
        /// Registra informacion sobre la consulta procesada.
        /// </summary>
        private void LogQuery(string query, string router, bool ruleMatched)
        {
            var status = ruleMatched ? "Regla encontrada" : "Sin regla - usando fallback";
            Console.WriteLine($"[MCP] Query: '{query}' | Router: {router} | Status: {status}");
        }

        /// <summary>
        /// Obtiene el servidor MCP asociado.
        /// </summary>
        public MCPServer Server => _server;
    }
}
