using System.Text.Json;
using Backend.MCP.Protocol;
using Backend.Persistence.Models;
using Backend.Persistence.MySQL;

namespace Backend.MCP.Server
{
    public class MagicCardMcpServer
    {
        private readonly MySQLRepository _repository;

        public MagicCardMcpServer(MySQLRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            CheckDbConnection();
        }

        private void CheckDbConnection()
        {
            Task.Run(async () =>
            {
                bool isConnected = await _repository.TestConnectionAsync();

                if (isConnected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n[MCP SERVER] ✅ CONEXIÓN EXITOSA CON MYSQL.");
                    Console.WriteLine("[MCP SERVER]    El sistema está listo para buscar cartas.\n");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[MCP SERVER] ❌ ERROR CRÍTICO: NO HAY CONEXIÓN CON MYSQL.");
                    Console.WriteLine("[MCP SERVER]    Verifica tu ConnectionString o si XAMPP/MySQL está corriendo.\n");
                }
                Console.ResetColor();
            });
        }

        public async Task<JsonRpcResponse> HandleRequestAsync(JsonRpcRequest request)
        {
            switch (request.Method)
            {
                case "tools/list":
                    return new JsonRpcResponse { Id = request.Id, Result = GetTools() };

                case "tools/call":
                    return await ExecuteToolAsync(request);

                default:
                    return new JsonRpcResponse { Id = request.Id, Error = "Method not found" };
            }
        }

        private ToolListResult GetTools()
        {
            return new ToolListResult
            {
                Tools = new List<McpTool>
                {
                    new McpTool
                    {
                        Name = "search_cards",
                        Description = "Busca cartas en MySQL.",
                        InputSchema = new
                        {
                            type = "object",
                            properties = new { name = new { type = "string", description = "Nombre o lista de nombres." } },
                            required = new[] { "name" }
                        }
                    },
                    new McpTool { Name = "get_statistics", Description = "Ver stats", InputSchema = new { type = "object", properties = new { } } }
                }
            };
        }

        private async Task<JsonRpcResponse> ExecuteToolAsync(JsonRpcRequest request)
        {
            try
            {
                var jsonString = JsonSerializer.Serialize(request.Params);
                var callParams = JsonSerializer.Deserialize<CallToolParams>(jsonString);

                string textResponse = "[]";

                if (callParams != null && callParams.Name == "search_cards")
                {
                    string rawTerm = "";
                    if (callParams.Arguments.TryGetValue("name", out object? nameObj))
                    {
                        if (nameObj is JsonElement element)
                            rawTerm = element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : element.GetRawText();
                        else
                            rawTerm = nameObj?.ToString() ?? "";

                        rawTerm = rawTerm.Trim().Trim('"');
                    }

                    if (!string.IsNullOrWhiteSpace(rawTerm))
                    {
                        var searchTerms = rawTerm.Split(',')
                                                 .Select(t => t.Trim())
                                                 .Where(t => !string.IsNullOrEmpty(t))
                                                 .ToList();

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[MCP QUERY] Buscando en MySQL: '{string.Join(", ", searchTerms)}'");
                        Console.ResetColor();

                        var resultCards = await _repository.SearchCardsAsync(searchTerms);

                        Console.WriteLine($"[MCP RESULT] Encontradas: {resultCards.Count} cartas.");

                        textResponse = resultCards.Any() ? JsonSerializer.Serialize(resultCards) : "[]";
                    }
                }
                else if (callParams != null && callParams.Name == "get_statistics")
                {
                    var all = await _repository.GetAllAsync();
                    textResponse = $"Total cartas en MySQL: {all.Count()}";
                }

                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new { content = new[] { new ToolContent { Type = "text", Text = textResponse } } }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP ERROR] {ex.Message}");
                return new JsonRpcResponse { Id = request.Id, Error = ex.Message };
            }
        }
    }
}