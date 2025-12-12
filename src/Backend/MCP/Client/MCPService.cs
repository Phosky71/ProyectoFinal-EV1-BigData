using System.Text;
using System.Text.Json;
using Backend.MCP.Protocol;
using Backend.MCP.Server;
using Backend.Persistence.Models;

namespace Backend.MCP.Client
{
    public class McpClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MagicCardMcpServer _mcpServer;
        private const string LOCAL_LLM_URL = "http://127.0.0.1:1234/v1/chat/completions";

        public McpClientService(IHttpClientFactory httpClientFactory, MagicCardMcpServer mcpServer)
        {
            _httpClientFactory = httpClientFactory;
            _mcpServer = mcpServer;
        }

        public async Task<(string Response, object? Data)> ProcessUserQueryAsync(string userQuery)
        {
            var toolsRequest = new JsonRpcRequest { Method = "tools/list", Id = 1 };
            var toolsResponse = await _mcpServer.HandleRequestAsync(toolsRequest);
            var toolsJson = JsonSerializer.Serialize(toolsResponse.Result);
            var toolsList = JsonSerializer.Deserialize<ToolListResult>(toolsJson);

            var systemPrompt = BuildSystemPrompt(toolsList);

            var llmResponse = await CallLlmAsync(systemPrompt, userQuery);

            if (TryDetectToolCall(llmResponse, out var toolName, out var toolArgs))
            {
                var callRequest = new JsonRpcRequest
                {
                    Method = "tools/call",
                    Id = 2,
                    Params = new CallToolParams { Name = toolName, Arguments = toolArgs }
                };

                var executionResponse = await _mcpServer.HandleRequestAsync(callRequest);

                List<Card>? dataForFrontend = null;
                string finalResponseText = "";

                try
                {

                    var resultJsonString = JsonSerializer.Serialize(executionResponse.Result);

                    using var doc = JsonDocument.Parse(resultJsonString);
                    var resultElement = doc.RootElement;

                    if (resultElement.TryGetProperty("content", out var contentArray) && contentArray.GetArrayLength() > 0)
                    {
                        var rawDataJson = contentArray[0].GetProperty("text").GetString() ?? "[]";

                        if (toolName == "search_cards")
                        {
                            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            dataForFrontend = JsonSerializer.Deserialize<List<Card>>(rawDataJson, opts);

                            if (dataForFrontend != null)
                            {
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine($"[CLIENT] Recibidas {dataForFrontend.Count} cartas desde MySQL.");
                                Console.ResetColor();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CLIENT ERROR] Error procesando JSON: {ex.Message}");
                }

                if (dataForFrontend == null || dataForFrontend.Count == 0)
                {
                    var promptNoData = $"PREGUNTA USUARIO: '{userQuery}'\n" +
                                       "CONTEXTO: La base de datos local NO tiene resultados.\n" +
                                       "INSTRUCCIÓN: Responde usando tu conocimiento general, pero avisa que no está en inventario.";

                    finalResponseText = await CallLlmAsync("Eres un experto en Magic.", promptNoData);
                }
                else
                {
                    var cardsDisplayBuilder = new StringBuilder();
                    cardsDisplayBuilder.AppendLine("\n---\n### 🔍 Resultados en Inventario:\n");

                    foreach (var card in dataForFrontend.Take(10))
                    {
                        cardsDisplayBuilder.AppendLine($"#### {card.Name}");

                        if (!string.IsNullOrWhiteSpace(card.ImageUrl))
                        {
                            cardsDisplayBuilder.AppendLine($"![{card.Name}]({card.ImageUrl})");
                        }
                        else
                        {
                            cardsDisplayBuilder.AppendLine("*(Imagen no disponible)*");
                        }

                        cardsDisplayBuilder.AppendLine($"- **Tipo:** {card.Type}");
                        cardsDisplayBuilder.AppendLine($"- **Coste:** {card.ManaCost}");
                        cardsDisplayBuilder.AppendLine($"- **Texto:** {card.Text}");
                        cardsDisplayBuilder.AppendLine("---");
                    }

                    if (dataForFrontend.Count > 10)
                        cardsDisplayBuilder.AppendLine($"\n*(... y {dataForFrontend.Count - 10} cartas más)*");

                    var promptIntro = $"El usuario buscó: '{userQuery}'.\n" +
                                      $"Encontradas {dataForFrontend.Count} cartas (Ej: {dataForFrontend[0].Name}).\n" +
                                      "INSTRUCCIÓN: Escribe SOLO una frase introductoria amable. NO listes cartas.";

                    var introLlm = await CallLlmAsync("Eres un asistente útil.", promptIntro);

                    finalResponseText = introLlm + "\n" + cardsDisplayBuilder.ToString();
                }

                return (finalResponseText, dataForFrontend);
            }

            return (llmResponse, null);
        }

        private string BuildSystemPrompt(ToolListResult? tools)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Eres un asistente experto conectado a una DB SQL de Magic.");
            sb.AppendLine("SI EL USUARIO PIDE GRUPOS (ej: 'Power 9', 'Mejores de Alpha'):");
            sb.AppendLine("1. Genera mentalmente la lista de nombres.");
            sb.AppendLine("2. LLAMA a 'search_cards' con los nombres SEPARADOS POR COMAS.");
            sb.AppendLine("Responde SOLO JSON válido.");

            if (tools != null)
            {
                sb.AppendLine("Tools:");
                foreach (var tool in tools.Tools) sb.AppendLine($"- {tool.Name}: {tool.Description}");
            }

            sb.AppendLine("Ejemplo: { \"tool\": \"search_cards\", \"arguments\": { \"name\": \"Black Lotus, Mox Pearl\" } }");
            return sb.ToString();
        }

        private bool TryDetectToolCall(string response, out string name, out Dictionary<string, object> args)
        {
            name = "";
            args = new Dictionary<string, object>();
            if (string.IsNullOrWhiteSpace(response)) return false;

            try
            {
                string clean = response.Replace("```json", "").Replace("```", "").Trim();
                int start = clean.IndexOf('{');
                int end = clean.LastIndexOf('}');
                if (start >= 0 && end > start)
                {
                    clean = clean.Substring(start, end - start + 1);
                    using var doc = JsonDocument.Parse(clean);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Name.Equals("tool", StringComparison.OrdinalIgnoreCase)) name = prop.Value.GetString() ?? "";
                        else if (prop.Name.Equals("arguments", StringComparison.OrdinalIgnoreCase))
                        {
                            var argsJson = prop.Value.GetRawText();
                            args = JsonSerializer.Deserialize<Dictionary<string, object>>(argsJson) ?? new();
                        }
                    }
                    return !string.IsNullOrEmpty(name);
                }
            }
            catch { }
            return false;
        }

        private async Task<string> CallLlmAsync(string system, string user)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            var requestBody = new
            {
                model = "local-model",
                messages = new[] { new { role = "system", content = system }, new { role = "user", content = user } },
                temperature = 0.1,
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var res = await httpClient.PostAsync(LOCAL_LLM_URL, content);
                if (res.IsSuccessStatusCode)
                {
                    var str = await res.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(str);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        return choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                }
            }
            catch { return "Error conexión LLM."; }
            return "";
        }
    }
}