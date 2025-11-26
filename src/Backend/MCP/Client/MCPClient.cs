using System;
using System.Threading.Tasks;
using ProyectoFinal.Backend.MCP.Routers;

namespace ProyectoFinal.Backend.MCP.Client
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

        public MCPClient(string llmEndpoint, string llmApiKey, string llmModel = "gpt-3.5-turbo")
        {
            _ruleRouter = new RuleRouter();
            _llmRouter = new LLMRouter(llmEndpoint, llmApiKey, llmModel);
            _server = new MCPServer();
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
                var ruleResult = await _ruleRouter.TryRouteAsync(query);
                
                if (ruleResult != null)
                {
                    LogQuery(query, "RuleRouter", true);
                    return ruleResult;
                }

                // Paso 2: Fallback a LLMRouter
                LogQuery(query, "LLMRouter", false);
                var llmResult = await _llmRouter.TryRouteAsync(query);
                
                return llmResult ?? "No se pudo procesar la consulta.";
            }
            catch (Exception ex)
            {
                return $"Error al procesar la consulta: {ex.Message}";
            }
        }

        /// <summary>
        /// Agrega una regla personalizada al RuleRouter.
        /// </summary>
        public void AddCustomRule(string keyword, Func<string, Task<string>> handler)
        {
            _ruleRouter.AddRule(keyword, handler);
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
