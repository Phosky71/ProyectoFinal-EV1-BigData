using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProyectoFinal.Backend.MCP.Routers
{
    /// <summary>
    /// Router basado en LLM (Large Language Model) para el protocolo MCP.
    /// Se ejecuta SOLO cuando el RuleRouter no encuentra ninguna regla que coincida.
    /// Procesa consultas en lenguaje natural usando un modelo de IA.
    /// </summary>
    public class LLMRouter : IRouter
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiEndpoint;
        private readonly string _apiKey;
        private readonly string _modelName;

        public LLMRouter(string apiEndpoint, string apiKey, string modelName = "gpt-3.5-turbo")
        {
            _httpClient = new HttpClient();
            _apiEndpoint = apiEndpoint;
            _apiKey = apiKey;
            _modelName = modelName;
        }

        /// <summary>
        /// Procesa la consulta usando el modelo LLM.
        /// Este metodo se llama cuando el RuleRouter retorna null.
        /// </summary>
        /// <param name="query">Consulta en lenguaje natural</param>
        /// <returns>Respuesta procesada por el LLM</returns>
        public async Task<string?> TryRouteAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            try
            {
                var response = await ProcessWithLLMAsync(query);
                return response;
            }
            catch (Exception ex)
            {
                // Log del error
                Console.WriteLine($"Error en LLMRouter: {ex.Message}");
                return $"Error al procesar la consulta: {ex.Message}";
            }
        }

        /// <summary>
        /// Procesa la consulta enviandola al servicio LLM.
        /// </summary>
        private async Task<string> ProcessWithLLMAsync(string query)
        {
            // TODO: Implementar la logica de comunicacion con el LLM
            // Este es un ejemplo de estructura para OpenAI API
            
            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "system", content = GetSystemPrompt() },
                    new { role = "user", content = query }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(_apiEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error del API: {response.StatusCode} - {responseContent}");
            }

            // TODO: Parsear la respuesta del LLM
            return ParseLLMResponse(responseContent);
        }

        /// <summary>
        /// Obtiene el prompt del sistema para contextualizar las consultas.
        /// </summary>
        private string GetSystemPrompt()
        {
            return @"Eres un asistente de base de datos. Tu objetivo es:
1. Interpretar consultas en lenguaje natural sobre datos
2. Generar respuestas utiles basadas en la informacion disponible
3. Si no puedes responder, indica claramente el motivo

Contexto: Sistema de gestion de datos con persistencia dual (MySQL/Memoria)";
        }

        /// <summary>
        /// Parsea la respuesta del servicio LLM.
        /// </summary>
        private string ParseLLMResponse(string responseJson)
        {
            // TODO: Implementar el parseo de la respuesta segun el proveedor LLM
            // Este es un ejemplo basico
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var choices = doc.RootElement.GetProperty("choices");
                var firstChoice = choices[0];
                var message = firstChoice.GetProperty("message");
                var content = message.GetProperty("content").GetString();
                return content ?? "Sin respuesta";
            }
            catch
            {
                return responseJson;
            }
        }

        /// <summary>
        /// El LLMRouter siempre puede manejar consultas (es el fallback).
        /// </summary>
        public bool CanHandle(string query)
        {
            return !string.IsNullOrWhiteSpace(query);
        }
    }
}
