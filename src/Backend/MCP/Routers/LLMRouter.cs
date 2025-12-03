using Backend.MCP.Interfaces;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.MCP.Routers
{
    /// <summary>
    /// Enrutador que usa un LLM (OpenAI GPT) para consultas complejas.
    /// Se ejecuta SOLO si RuleRouter no encuentra una regla.
    /// </summary>
    public class LLMRouter : ILLMRouter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _apiKey;

        public LLMRouter(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        /// <summary>
        /// Procesa una consulta usando OpenAI GPT.
        /// </summary>
        public async Task<RouterResult> ProcessAsync(string query, IRepository<Card> repository)
        {
            // Verificar si la API key está configurada
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_OPENAI_API_KEY_HERE")
            {
                return new RouterResult
                {
                    Success = true,
                    Response = "LLM no configurado. Usa el RuleRouter con comandos como 'count cards', 'find [nombre]', etc.",
                    Data = null
                };
            }

            try
            {
                // Obtener contexto de la base de datos
                var cards = await repository.GetAllAsync();
                var context = BuildContext(cards);

                // Llamar a OpenAI
                var llmResponse = await CallOpenAIAsync(query, context);

                // Intentar extraer cartas relevantes basándose en la respuesta
                var relevantCards = ExtractRelevantCards(llmResponse, cards);

                return new RouterResult
                {
                    Success = true,
                    Response = llmResponse,
                    Data = relevantCards
                };
            }
            catch (Exception ex)
            {
                return new RouterResult
                {
                    Success = true,
                    Response = $"Error en LLM: {ex.Message}. Intenta con comandos como 'count cards' o 'find [nombre]'.",
                    Data = null
                };
            }
        }

        // ==================== MÉTODOS PRIVADOS ====================

        private async Task<string> CallOpenAIAsync(string query, string context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Eres un asistente experto en Magic: The Gathering. Responde preguntas sobre cartas basándote en el contexto proporcionado. " +
                                 "Si la pregunta no se puede responder con el contexto, sugiere usar comandos como 'count cards', 'find [nombre]', 'blue cards', etc."
                    },
                    new
                    {
                        role = "user",
                        content = $"Contexto de la base de datos:\n{context}\n\nPregunta: {query}"
                    }
                },
                max_tokens = 150,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI API error: {response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseString);

            return responseObj.GetProperty("choices")[0]
                             .GetProperty("message")
                             .GetProperty("content")
                             .GetString() ?? "No response from LLM";
        }

        private string BuildContext(IEnumerable<Card> cards)
        {
            var cardsList = cards.Take(20).ToList(); // Limitar contexto

            if (!cardsList.Any())
            {
                return "Base de datos vacía.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Total de cartas: {cards.Count()}");
            sb.AppendLine($"Ejemplo de cartas:");

            foreach (var card in cardsList.Take(5))
            {
                sb.AppendLine($"- {card.Name} ({card.Type}, {card.Rarity})");
            }

            return sb.ToString();
        }

        private List<Card> ExtractRelevantCards(string llmResponse, IEnumerable<Card> allCards)
        {
            // Buscar nombres de cartas mencionados en la respuesta del LLM
            var relevantCards = new List<Card>();

            foreach (var card in allCards)
            {
                if (!string.IsNullOrEmpty(card.Name) &&
                    llmResponse.Contains(card.Name, StringComparison.OrdinalIgnoreCase))
                {
                    relevantCards.Add(card);

                    if (relevantCards.Count >= 5) break;
                }
            }

            return relevantCards;
        }
    }
}
