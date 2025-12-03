using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Backend.MCP.Interfaces;
using Backend.MCP.Routers;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;

namespace Backend.MCP
{
    /// <summary>
    /// Implementación del servicio MCP que coordina RuleRouter y LLMRouter.
    /// </summary>
    public class MCPService : IMCPService
    {
        private readonly IRuleRouter _ruleRouter;
        private readonly ILLMRouter _llmRouter;
        private readonly IRepository<Card> _repository;

        public MCPService(
            IRuleRouter ruleRouter,
            ILLMRouter llmRouter,
            IRepository<Card> repository)
        {
            _ruleRouter = ruleRouter;
            _llmRouter = llmRouter;
            _repository = repository;
        }

        /// <summary>
        /// Procesa una consulta: primero intenta con RuleRouter, luego con LLMRouter.
        /// </summary>
        public async Task<MCPResult> ProcessQueryAsync(string query)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. Intentar primero con RuleRouter (reglas manuales)
                var ruleResult = await _ruleRouter.ProcessAsync(query);

                if (ruleResult.Success)
                {
                    stopwatch.Stop();

                    return new MCPResult
                    {
                        Response = ruleResult.Response,
                        RouterUsed = "Rule",
                        ResultCount = ruleResult.Data?.Count ?? 0,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        Data = ruleResult.Data
                    };
                }

                // 2. Si no hay regla, usar LLMRouter
                var llmResult = await _llmRouter.ProcessAsync(query, _repository);

                stopwatch.Stop();

                return new MCPResult
                {
                    Response = llmResult.Response,
                    RouterUsed = "LLM",
                    ResultCount = llmResult.Data?.Count ?? 0,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Data = llmResult.Data
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                return new MCPResult
                {
                    Response = $"Error processing query: {ex.Message}",
                    RouterUsed = "Error",
                    ResultCount = 0,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Data = null
                };
            }
        }

        /// <summary>
        /// Obtiene la lista de reglas disponibles en el RuleRouter.
        /// </summary>
        public List<string> GetAvailableRules()
        {
            return _ruleRouter.GetAvailableRules();
        }
    }
}

