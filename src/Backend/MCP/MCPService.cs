using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Backend.MCP.Interfaces;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;

namespace Backend.MCP
{
    /// <summary>
    /// Implementación del servicio MCP que coordina RuleRouter y LLMRouter.
    /// Cumple con el requisito del enunciado de tener 2 enrutadores en cascada.
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
            _ruleRouter = ruleRouter ?? throw new ArgumentNullException(nameof(ruleRouter));
            _llmRouter = llmRouter ?? throw new ArgumentNullException(nameof(llmRouter));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Procesa una consulta en lenguaje natural.
        /// 1. Intenta primero con RuleRouter (reglas manuales) - se ejecuta primero según enunciado.
        /// 2. Si no encuentra reglas, usa LLMRouter (IA) - se ejecuta como fallback.
        /// </summary>
        public async Task<MCPResult> ProcessQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new MCPResult
                {
                    Response = "La consulta no puede estar vacía",
                    RouterUsed = "Error",
                    ResultCount = 0,
                    ExecutionTimeMs = 0,
                    Data = null
                };
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                Console.WriteLine($"[MCP] Processing query: '{query}'");

                // 1. PRIMER ENRUTADOR: RuleRouter (reglas manuales)
                Console.WriteLine("[MCP] Trying RuleRouter first...");
                var ruleResult = await _ruleRouter.ProcessAsync(query);

                if (ruleResult.Success)
                {
                    stopwatch.Stop();
                    Console.WriteLine($"[MCP] RuleRouter matched! Response: {ruleResult.Response}");

                    return new MCPResult
                    {
                        Response = ruleResult.Response,
                        RouterUsed = "Rule",
                        ResultCount = ruleResult.Data?.Count ?? 0,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        Data = ruleResult.Data
                    };
                }

                // 2. SEGUNDO ENRUTADOR: LLMRouter (IA como fallback)
                Console.WriteLine("[MCP] No rule matched, using LLMRouter...");
                var llmResult = await _llmRouter.ProcessAsync(query, _repository);

                stopwatch.Stop();
                Console.WriteLine($"[MCP] LLMRouter response: {llmResult.Response}");

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
                Console.WriteLine($"[MCP] Error: {ex.Message}");

                return new MCPResult
                {
                    Response = $"Error al procesar la consulta: {ex.Message}",
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
            try
            {
                return _ruleRouter.GetAvailableRules();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP] Error getting rules: {ex.Message}");
                return new List<string> { "Error: No se pudieron obtener las reglas" };
            }
        }
    }
}
