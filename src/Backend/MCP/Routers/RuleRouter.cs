using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoFinal.Backend.MCP.Routers
{
    /// <summary>
    /// Router basado en reglas manuales para el protocolo MCP.
    /// Se ejecuta PRIMERO antes que el LLMRouter.
    /// Si encuentra una regla que coincide, la ejecuta.
    /// Si no encuentra ninguna regla, retorna null para que el LLMRouter procese la consulta.
    /// </summary>
    public class RuleRouter : IRouter
    {
        private readonly Dictionary<string, Func<string, Task<string>>> _rules;

        public RuleRouter()
        {
            _rules = new Dictionary<string, Func<string, Task<string>>>(StringComparer.OrdinalIgnoreCase);
            InitializeDefaultRules();
        }

        /// <summary>
        /// Inicializa las reglas por defecto del router.
        /// </summary>
        private void InitializeDefaultRules()
        {
            // TODO: Agregar reglas manuales aqui
            // Ejemplo:
            // _rules.Add("listar usuarios", async (query) => await ListUsersAsync());
            // _rules.Add("contar registros", async (query) => await CountRecordsAsync());
        }

        /// <summary>
        /// Agrega una nueva regla al router.
        /// </summary>
        /// <param name="keyword">Palabra clave que activa la regla</param>
        /// <param name="handler">Funcion que maneja la regla</param>
        public void AddRule(string keyword, Func<string, Task<string>> handler)
        {
            if (!_rules.ContainsKey(keyword))
            {
                _rules.Add(keyword, handler);
            }
        }

        /// <summary>
        /// Intenta enrutar la consulta usando las reglas definidas.
        /// </summary>
        /// <param name="query">Consulta en lenguaje natural</param>
        /// <returns>Resultado si encuentra regla, null si no hay coincidencia</returns>
        public async Task<string?> TryRouteAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            foreach (var rule in _rules)
            {
                if (query.Contains(rule.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return await rule.Value(query);
                }
            }

            // No se encontro ninguna regla - retornar null para que LLMRouter procese
            return null;
        }

        /// <summary>
        /// Indica si el router puede manejar la consulta.
        /// </summary>
        public bool CanHandle(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            foreach (var rule in _rules)
            {
                if (query.Contains(rule.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Interface para los routers MCP.
    /// </summary>
    public interface IRouter
    {
        Task<string?> TryRouteAsync(string query);
        bool CanHandle(string query);
    }
}
