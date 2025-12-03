using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Backend.MCP.Interfaces;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;

namespace Backend.MCP.Routers
{
    /// <summary>
    /// Enrutador con reglas manuales para consultas sobre cartas de Magic.
    /// Se ejecuta PRIMERO antes del LLMRouter.
    /// </summary>
    public class RuleRouter : IRuleRouter
    {
        private readonly IRepository<Card> _repository;

        // Diccionario de reglas: patrón regex -> función de procesamiento
        private readonly Dictionary<string, Func<string, Task<RouterResult>>> _rules;

        public RuleRouter(IRepository<Card> repository)
        {
            _repository = repository;
            _rules = InitializeRules();
        }

        /// <summary>
        /// Procesa una consulta usando reglas manuales.
        /// </summary>
        public async Task<RouterResult> ProcessAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new RouterResult
                {
                    Success = false,
                    Response = "Empty query"
                };
            }

            // Buscar una regla que coincida
            foreach (var rule in _rules)
            {
                if (Regex.IsMatch(query, rule.Key, RegexOptions.IgnoreCase))
                {
                    return await rule.Value(query);
                }
            }

            // No hay regla que coincida
            return new RouterResult
            {
                Success = false,
                Response = "No rule found for this query"
            };
        }

        /// <summary>
        /// Obtiene la lista de reglas disponibles.
        /// </summary>
        public List<string> GetAvailableRules()
        {
            return new List<string>
            {
                "¿Cuántas cartas hay? / count cards / total cards",
                "Buscar cartas por nombre: 'find [nombre]' / 'search [nombre]'",
                "Cartas de un color: 'blue cards' / 'red creatures' / 'white spells'",
                "Cartas de una rareza: 'rare cards' / 'mythic cards' / 'common cards'",
                "Cartas de un set: 'cards from [set name]' / 'set [nombre]'",
                "Criaturas: 'show creatures' / 'list creatures'",
                "Cartas con poder/resistencia: 'creatures with power 5' / '3/3 creatures'"
            };
        }

        // ==================== REGLAS MANUALES ====================

        private Dictionary<string, Func<string, Task<RouterResult>>> InitializeRules()
        {
            return new Dictionary<string, Func<string, Task<RouterResult>>>
            {
                // Regla 1: Contar cartas totales
                [@"\b(count|total|cuántas|cuantas)\b.*\b(cards|cartas)\b"] = async (query) =>
                {
                    var cards = await _repository.GetAllAsync();
                    var count = cards.Count();
                    return new RouterResult
                    {
                        Success = true,
                        Response = $"Total de cartas en la base de datos: {count}",
                        Data = null
                    };
                },

                // Regla 2: Buscar por nombre
                [@"\b(find|search|busca|buscar|show|muestra)\b\s+(.+)"] = async (query) =>
                {
                    var match = Regex.Match(query, @"\b(find|search|busca|buscar|show|muestra)\b\s+(.+)", RegexOptions.IgnoreCase);
                    if (!match.Success) return FailResult();

                    var searchTerm = match.Groups[2].Value.Trim();
                    var cards = await _repository.GetAllAsync();
                    var found = cards.Where(c =>
                        c.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true
                    ).Take(10).ToList();

                    if (!found.Any())
                    {
                        return new RouterResult
                        {
                            Success = true,
                            Response = $"No se encontraron cartas con el nombre '{searchTerm}'",
                            Data = new List<Card>()
                        };
                    }

                    return new RouterResult
                    {
                        Success = true,
                        Response = $"Encontradas {found.Count} cartas con '{searchTerm}'",
                        Data = found
                    };
                },

                // Regla 3: Filtrar por color
                [@"\b(white|blue|black|red|green|blanco|azul|negro|rojo|verde)\b.*\b(cards|cartas|creatures|criaturas)\b"] = async (query) =>
                {
                    var colorMap = new Dictionary<string, string[]>
                    {
                        { "white", new[] { "W", "blanco" } },
                        { "blue", new[] { "U", "azul" } },
                        { "black", new[] { "B", "negro" } },
                        { "red", new[] { "R", "rojo" } },
                        { "green", new[] { "G", "verde" } }
                    };

                    foreach (var color in colorMap)
                    {
                        if (Regex.IsMatch(query, $@"\b({string.Join("|", color.Value)})\b", RegexOptions.IgnoreCase))
                        {
                            var cards = await _repository.GetAllAsync();
                            var filtered = cards.Where(c =>
                                c.ManaCost?.Contains($"{{{color.Key[0].ToString().ToUpper()}}}") == true
                            ).Take(10).ToList();

                            return new RouterResult
                            {
                                Success = true,
                                Response = $"Encontradas {filtered.Count} cartas {color.Key}",
                                Data = filtered
                            };
                        }
                    }

                    return FailResult();
                },

                // Regla 4: Filtrar por rareza
                [@"\b(common|uncommon|rare|mythic|común|infrecuente|rara|mítica)\b.*\b(cards|cartas)\b"] = async (query) =>
                {
                    var rarityMap = new Dictionary<string, string[]>
                    {
                        { "Common", new[] { "common", "común" } },
                        { "Uncommon", new[] { "uncommon", "infrecuente" } },
                        { "Rare", new[] { "rare", "rara" } },
                        { "Mythic", new[] { "mythic", "mítica" } }
                    };

                    foreach (var rarity in rarityMap)
                    {
                        if (Regex.IsMatch(query, $@"\b({string.Join("|", rarity.Value)})\b", RegexOptions.IgnoreCase))
                        {
                            var cards = await _repository.GetAllAsync();
                            var filtered = cards.Where(c =>
                                c.Rarity?.Equals(rarity.Key, StringComparison.OrdinalIgnoreCase) == true
                            ).Take(10).ToList();

                            return new RouterResult
                            {
                                Success = true,
                                Response = $"Encontradas {filtered.Count} cartas {rarity.Key}",
                                Data = filtered
                            };
                        }
                    }

                    return FailResult();
                },

                // Regla 5: Mostrar criaturas
                [@"\b(creature|creatures|criatura|criaturas)\b"] = async (query) =>
                {
                    var cards = await _repository.GetAllAsync();
                    var creatures = cards.Where(c =>
                        c.Type?.Contains("Creature", StringComparison.OrdinalIgnoreCase) == true
                    ).Take(10).ToList();

                    return new RouterResult
                    {
                        Success = true,
                        Response = $"Encontradas {creatures.Count} criaturas",
                        Data = creatures
                    };
                },

                // Regla 6: Filtrar por set
                [@"\b(set|expansion|expansión)\b\s+(.+)"] = async (query) =>
                {
                    var match = Regex.Match(query, @"\b(set|expansion|expansión)\b\s+(.+)", RegexOptions.IgnoreCase);
                    if (!match.Success) return FailResult();

                    var setName = match.Groups[2].Value.Trim();
                    var cards = await _repository.GetAllAsync();
                    var filtered = cards.Where(c =>
                        c.SetName?.Contains(setName, StringComparison.OrdinalIgnoreCase) == true
                    ).Take(10).ToList();

                    return new RouterResult
                    {
                        Success = true,
                        Response = $"Encontradas {filtered.Count} cartas del set '{setName}'",
                        Data = filtered
                    };
                }
            };
        }

        private RouterResult FailResult()
        {
            return new RouterResult
            {
                Success = false,
                Response = "Could not process rule"
            };
        }
    }
}
