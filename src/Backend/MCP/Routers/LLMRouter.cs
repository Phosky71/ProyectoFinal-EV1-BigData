using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.MCP.Interfaces;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;

namespace Backend.MCP.Routers
{
    /// <summary>
    /// Enrutador que usa Google Gemini para consultas complejas.
    /// Se ejecuta SOLO si RuleRouter no encuentra una regla (segundo enrutador según enunciado).
    /// </summary>
    public class LLMRouter : ILLMRouter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _apiKey;
        private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

        public LLMRouter(IHttpClientFactory httpClientFactory, string? apiKey)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _apiKey = apiKey;
        }

        /// <summary>
        /// Procesa una consulta usando Google Gemini.
        /// Genera respuestas en lenguaje natural basándose en el contexto de las cartas.
        /// </summary>
        public async Task<RouterResult> ProcessAsync(string query, IRepository<Card> repository)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new RouterResult
                {
                    Success = false,
                    Response = "La consulta está vacía"
                };
            }

            // Verificar si la API key está configurada
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                Console.WriteLine("[LLMRouter] Google Gemini API key not configured, using fallback");

                return new RouterResult
                {
                    Success = true,
                    Response = $"Entiendo que preguntas: '{query}'. Sin embargo, el LLM no está configurado. " +
                              "Para obtener respuestas inteligentes, configura una API key de Google Gemini en appsettings.json. " +
                              "Mientras tanto, intenta usar comandos específicos como: 'cuántas cartas hay', 'busca [nombre]', 'cartas azules', etc.",
                    Data = null
                };
            }

            try
            {
                Console.WriteLine($"[LLMRouter] Calling Google Gemini API for query: '{query}'");

                // Obtener contexto de la base de datos
                var cards = await repository.GetAllAsync();
                var cardsList = cards.ToList();
                var context = BuildContext(cardsList);

                // Llamar a Google Gemini
                var llmResponse = await CallGeminiAsync(query, context);

                // Intentar extraer cartas relevantes basándose en la respuesta
                var relevantCards = ExtractRelevantCards(llmResponse, cardsList);

                Console.WriteLine($"[LLMRouter] Gemini response: {llmResponse}");
                Console.WriteLine($"[LLMRouter] Found {relevantCards.Count} relevant cards");

                return new RouterResult
                {
                    Success = true,
                    Response = llmResponse,
                    Data = relevantCards.Any() ? relevantCards : null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LLMRouter] Error: {ex.Message}");

                return new RouterResult
                {
                    Success = true,
                    Response = $"Ocurrió un error al consultar la IA: {ex.Message}. " +
                              "Intenta con comandos más específicos como: 'count cards', 'find [nombre]', 'blue cards'.",
                    Data = null
                };
            }
        }

        // ==================== MÉTODOS PRIVADOS ====================

        /// <summary>
        /// Llama a la API de Google Gemini.
        /// </summary>
        private async Task<string> CallGeminiAsync(string query, string context)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var systemPrompt = "Eres un asistente experto en Magic: The Gathering. " +
                              "Responde preguntas sobre cartas basándote en el contexto proporcionado. " +
                              "Sé conciso y directo. Si mencionas cartas específicas, usa sus nombres exactos. " +
                              "Si no puedes responder con certeza, sugiere usar comandos específicos.";

            var fullPrompt = $"{systemPrompt}\n\n" +
                           $"Contexto de la base de datos:\n{context}\n\n" +
                           $"Pregunta del usuario: {query}\n\n" +
                           $"Respuesta:";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = fullPrompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 500,
                    topP = 0.95,
                    topK = 40
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{GEMINI_API_URL}?key={_apiKey}";
            var response = await httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Google Gemini API error ({response.StatusCode}): {errorContent}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseString);

            // Parsear la respuesta de Gemini
            var candidates = responseObj.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                return "No se obtuvo respuesta de Gemini";
            }

            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "No se obtuvo respuesta del LLM";
        }

        /// <summary>
        /// Construye un contexto resumido de las cartas para el LLM.
        /// </summary>
        private string BuildContext(List<Card> cards)
        {
            if (!cards.Any())
            {
                return "La base de datos está vacía.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Total de cartas: {cards.Count}");

            // Estadísticas por rareza
            var byRarity = cards.Where(c => !string.IsNullOrEmpty(c.Rarity))
                .GroupBy(c => c.Rarity)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();

            if (byRarity.Any())
            {
                sb.AppendLine($"Distribución por rareza: {string.Join(", ", byRarity)}");
            }

            // Colores disponibles
            var colors = cards.Where(c => !string.IsNullOrEmpty(c.ManaCost))
                .Select(c => c.GetColors())
                .Distinct()
                .Take(5)
                .ToList();

            if (colors.Any())
            {
                sb.AppendLine($"Colores: {string.Join(", ", colors)}");
            }

            // Sets principales
            var sets = cards.Where(c => !string.IsNullOrEmpty(c.SetName))
                .GroupBy(c => c.SetName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            if (sets.Any())
            {
                sb.AppendLine($"Sets: {string.Join(", ", sets)}");
            }

            // Ejemplos de cartas
            sb.AppendLine("\nEjemplos:");
            foreach (var card in cards.Take(10))
            {
                sb.AppendLine($"- {card.Name} ({card.Type ?? "Unknown"}, {card.Rarity ?? "Unknown"})");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Extrae cartas relevantes mencionadas en la respuesta del LLM.
        /// </summary>
        private List<Card> ExtractRelevantCards(string llmResponse, List<Card> allCards)
        {
            var relevantCards = new List<Card>();

            foreach (var card in allCards)
            {
                if (!string.IsNullOrEmpty(card.Name) &&
                    llmResponse.Contains(card.Name, StringComparison.OrdinalIgnoreCase))
                {
                    relevantCards.Add(card);
                    if (relevantCards.Count >= 10) break;
                }
            }

            return relevantCards;
        }
    }
}
