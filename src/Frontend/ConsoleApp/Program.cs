using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Frontend.ConsoleApp
{
    class Program
    {
        private static HttpClient _httpClient = null!;
        private static string? _jwtToken;
        private static readonly string _baseUrl = "https://localhost:53620/api"; 

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine("║  Proyecto Final EV1 - Console Client      ║");
            Console.WriteLine("║  Magic: The Gathering Card Manager        ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60) 
            };

            if (!await LoginAsync())
            {
                Console.WriteLine("\nLogin failed. Press any key to exit...");
                Console.ReadKey();
                return;
            }

            await ShowMainMenuAsync();
        }

        static async Task ShowMainMenuAsync()
        {
            bool exit = false;

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("\n" + new string('═', 50));
                Console.WriteLine("MAIN MENU");
                Console.WriteLine(new string('═', 50));
                Console.WriteLine("1.  List All Cards");
                Console.WriteLine("2.  Find Card by ID");
                Console.WriteLine("3.  Create New Card");
                Console.WriteLine("4.  Update Card");
                Console.WriteLine("5.  Delete Card");
                Console.WriteLine("6.  Switch Persistence Mode");
                Console.WriteLine("7.  Load Data from Kaggle CSV");
                Console.WriteLine("8.  View Statistics");
                Console.WriteLine("9.  MCP Query (Natural Language)");
                Console.WriteLine("0.  Exit");
                Console.WriteLine(new string('═', 50));
                Console.Write("Select option: ");

                var choice = Console.ReadLine()?.Trim();

                try
                {
                    switch (choice)
                    {
                        case "1": await ListCardsAsync(); break;
                        case "2": await FindCardAsync(); break;
                        case "3": await CreateCardAsync(); break;
                        case "4": await UpdateCardAsync(); break;
                        case "5": await DeleteCardAsync(); break;
                        case "6": await SwitchPersistenceAsync(); break;
                        case "7": await LoadDataFromKaggleAsync(); break;
                        case "8": await ViewStatisticsAsync(); break;
                        case "9": await MCPQueryAsync(); break;
                        case "0":
                            exit = true;
                            Console.WriteLine("\nGoodbye!");
                            break;
                        default:
                            Console.WriteLine("\nInvalid option. Try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                    }
                }

                if (!exit)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        // ==================== LOGIN ====================

        private static async Task<bool> LoginAsync()
        {
            Console.WriteLine("LOGIN");
            Console.WriteLine("──────────────────────────────────────────────────");

            Console.Write("Username (default: admin): ");
            var username = Console.ReadLine();
            if (string.IsNullOrEmpty(username)) username = "admin";

            Console.Write("Password (default: admin123): ");
            var password = ReadPassword();
            if (string.IsNullOrEmpty(password)) password = "admin123";
            Console.WriteLine();

            try
            {
                var loginRequest = new { username, password };
                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/Auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(responseJson);
                    var root = document.RootElement;

                    _jwtToken = root.GetProperty("token").GetString();
                    var user = root.GetProperty("username").GetString();
                    var role = root.GetProperty("role").GetString();

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");

                    Console.WriteLine("\nLogin successful!");
                    Console.WriteLine($"   User: {user}");
                    Console.WriteLine($"   Role: {role}");

                    return true;
                }
                else
                {
                    Console.WriteLine($"\nLogin failed: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nConnection error: {ex.Message}");
                return false;
            }
        }


        // ==================== CRUD OPERATIONS ====================

        static async Task ListCardsAsync()
        {
            Console.WriteLine("\nLISTING ALL CARDS");
            Console.WriteLine(new string('─', 50));

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/data");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(json);
                    var count = result.GetProperty("count").GetInt32();
                    var data = result.GetProperty("data");

                    Console.WriteLine($"\nTotal cards: {count}\n");

                    if (count == 0)
                    {
                        Console.WriteLine("   No cards found. Load data from Kaggle first (option 7).");
                        return;
                    }

                    int index = 1;
                    foreach (var card in data.EnumerateArray())
                    {
                        var id = GetPropertyOrDefault(card, "id", "N/A");
                        var name = GetPropertyOrDefault(card, "name", "Unnamed");
                        var type = GetPropertyOrDefault(card, "type", "Unknown");
                        var rarity = GetPropertyOrDefault(card, "rarity", "Unknown");

                        Console.WriteLine($"{index,3}. [{id}] {name}");
                        Console.WriteLine($"     Type: {type} | Rarity: {rarity}");
                        index++;

                        if (index > 20)
                        {
                            Console.WriteLine($"\n... and {count - 20} more cards (use search to find specific ones)");
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                    Console.WriteLine($"   {json}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task FindCardAsync()
        {
            Console.Write("\nEnter Card ID (UUID): ");
            string? id = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine("ID cannot be empty");
                return;
            }

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/data/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var card = JsonSerializer.Deserialize<JsonElement>(json);

                    Console.WriteLine("\nCard found:");
                    Console.WriteLine(new string('─', 50));
                    Console.WriteLine($"ID:        {GetPropertyOrDefault(card, "id")}");
                    Console.WriteLine($"Name:      {GetPropertyOrDefault(card, "name")}");
                    Console.WriteLine($"Type:      {GetPropertyOrDefault(card, "type")}");
                    Console.WriteLine($"Mana Cost: {GetPropertyOrDefault(card, "manaCost")}");
                    Console.WriteLine($"Rarity:    {GetPropertyOrDefault(card, "rarity")}");
                    Console.WriteLine($"Set:       {GetPropertyOrDefault(card, "setName")}");

                    var power = GetPropertyOrDefault(card, "power", "");
                    var toughness = GetPropertyOrDefault(card, "toughness", "");
                    if (!string.IsNullOrEmpty(power) && !string.IsNullOrEmpty(toughness))
                    {
                        Console.WriteLine($"P/T:       {power}/{toughness}");
                    }

                    var text = GetPropertyOrDefault(card, "text", "");
                    if (!string.IsNullOrEmpty(text) && text != "N/A")
                    {
                        Console.WriteLine($"\nText: {text}");
                    }
                }
                else
                {
                    Console.WriteLine($"Card not found (ID: {id})");
                    Console.WriteLine($"   {json}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task CreateCardAsync()
        {
            Console.WriteLine("\nCREATE NEW CARD");
            Console.WriteLine(new string('─', 50));

            Console.Write("Name: ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Name is required");
                return;
            }

            Console.Write("Type (e.g., Creature — Human Wizard): ");
            var type = Console.ReadLine()?.Trim();

            Console.Write("Mana Cost (e.g., {2}{U}{U}): ");
            var manaCost = Console.ReadLine()?.Trim();

            Console.Write("Rarity (Common/Uncommon/Rare/Mythic): ");
            var rarity = Console.ReadLine()?.Trim();

            Console.Write("Set Name: ");
            var setName = Console.ReadLine()?.Trim();

            var card = new
            {
                name,
                type,
                manaCost,
                rarity,
                setName
            };

            try
            {
                var json = JsonSerializer.Serialize(card);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/data", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var created = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    var id = GetPropertyOrDefault(created, "id");

                    Console.WriteLine($"\nCard created successfully!");
                    Console.WriteLine($"   ID: {id}");
                    Console.WriteLine($"   Name: {name}");
                }
                else
                {
                    Console.WriteLine($"\nError creating card:");
                    Console.WriteLine($"   {responseJson}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task UpdateCardAsync()
        {
            Console.Write("\nEnter Card ID to update: ");
            string? id = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine("ID cannot be empty");
                return;
            }

            var getResponse = await _httpClient.GetAsync($"{_baseUrl}/data/{id}");
            if (!getResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Card not found (ID: {id})");
                return;
            }

            var existingJson = await getResponse.Content.ReadAsStringAsync();
            var existing = JsonSerializer.Deserialize<JsonElement>(existingJson);

            Console.WriteLine("\nCurrent values (press Enter to keep):");
            Console.WriteLine(new string('─', 50));

            var currentName = GetPropertyOrDefault(existing, "name");
            Console.Write($"Name [{currentName}]: ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) name = currentName;

            var currentType = GetPropertyOrDefault(existing, "type");
            Console.Write($"Type [{currentType}]: ");
            var type = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(type)) type = currentType;

            var currentManaCost = GetPropertyOrDefault(existing, "manaCost");
            Console.Write($"Mana Cost [{currentManaCost}]: ");
            var manaCost = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(manaCost)) manaCost = currentManaCost;

            var currentRarity = GetPropertyOrDefault(existing, "rarity");
            Console.Write($"Rarity [{currentRarity}]: ");
            var rarity = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(rarity)) rarity = currentRarity;

            var updated = new
            {
                id,
                name,
                type,
                manaCost,
                rarity,
                setName = GetPropertyOrDefault(existing, "setName")
            };

            try
            {
                var json = JsonSerializer.Serialize(updated);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}/data/{id}", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("\nCard updated successfully!");
                }
                else
                {
                    Console.WriteLine($"\nError updating card:");
                    Console.WriteLine($"   {responseJson}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task DeleteCardAsync()
        {
            Console.Write("\nEnter Card ID to delete: ");
            string? id = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine("ID cannot be empty");
                return;
            }

            var getResponse = await _httpClient.GetAsync($"{_baseUrl}/data/{id}");
            if (!getResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Card not found (ID: {id})");
                return;
            }

            var cardJson = await getResponse.Content.ReadAsStringAsync();
            var card = JsonSerializer.Deserialize<JsonElement>(cardJson);
            var cardName = GetPropertyOrDefault(card, "name");

            Console.Write($"\nDelete '{cardName}' (ID: {id})? (y/N): ");
            var confirm = Console.ReadLine()?.Trim().ToLower();

            if (confirm != "y" && confirm != "yes")
            {
                Console.WriteLine("Deletion cancelled");
                return;
            }

            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/data/{id}");
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"\nCard '{cardName}' deleted successfully!");
                }
                else
                {
                    Console.WriteLine($"\nError deleting card:");
                    Console.WriteLine($"   {responseJson}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        // ==================== CONFIGURATION ====================

        static async Task SwitchPersistenceAsync()
        {
            Console.WriteLine("\nSWITCH PERSISTENCE MODE");
            Console.WriteLine(new string('─', 50));
            Console.WriteLine("1. Memory (LINQ in-memory)");
            Console.WriteLine("2. MySQL (Database)");
            Console.Write("\nSelect mode: ");

            string? choice = Console.ReadLine()?.Trim();
            string mode = choice == "2" ? "MySQL" : "Memory";

            var request = new { mode };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/data/switch-persistence", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    var message = GetPropertyOrDefault(result, "message");
                    var newMode = GetPropertyOrDefault(result, "currentMode");

                    Console.WriteLine($"\n{message}");
                    Console.WriteLine($"   Current mode: {newMode}");
                }
                else
                {
                    Console.WriteLine($"\nError:");
                    Console.WriteLine($"   {responseJson}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        // ==================== KAGGLE DATA LOADING ====================

        static async Task LoadDataFromKaggleAsync()
        {
            Console.WriteLine("\nLOAD DATA FROM KAGGLE CSV");
            Console.WriteLine(new string('─', 50));
            Console.WriteLine("This will load cards from: Data/cards.csv");
            Console.WriteLine("   Make sure the CSV file exists in the API project.");
            Console.Write("\nProceed? (y/N): ");

            var confirm = Console.ReadLine()?.Trim().ToLower();
            if (confirm != "y" && confirm != "yes")
            {
                Console.WriteLine("Operation cancelled");
                return;
            }

            try
            {
                Console.WriteLine("\nLoading data... This may take 5-10 minutes for large files...");

                using var longTimeoutClient = new HttpClient
                {
                    BaseAddress = new Uri(_baseUrl),
                    Timeout = TimeSpan.FromMinutes(10) 
                };

                if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                {
                    var authHeader = _httpClient.DefaultRequestHeaders.GetValues("Authorization").FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        longTimeoutClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                    }
                }

                var response = await longTimeoutClient.PostAsync("/data/load-kaggle", null);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(json);
                    var message = GetPropertyOrDefault(result, "message");
                    var recordsLoaded = result.GetProperty("recordsLoaded").GetInt32();
                    var persistenceMode = GetPropertyOrDefault(result, "persistenceMode");

                    Console.WriteLine("\nData loaded successfully!");
                    Console.WriteLine($"   {message}");
                    Console.WriteLine($"   Records: {recordsLoaded:N0}");
                    Console.WriteLine($"   Mode: {persistenceMode}");
                }
                else
                {
                    Console.WriteLine($"\nError loading data:");
                    Console.WriteLine($"   {json}");
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("\nRequest timeout. The CSV file might be too large.");
                Console.WriteLine("   Try increasing the timeout or use a smaller dataset.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }


        private static async Task ViewStatisticsAsync()
        {
            Console.WriteLine("\nSTATISTICS");
            Console.WriteLine("──────────────────────────────────────────────────");

            try
            {
                if (string.IsNullOrEmpty(_jwtToken))
                {
                    Console.WriteLine("Not authenticated. Please login first.");
                    return;
                }

                var response = await _httpClient.GetAsync($"{_baseUrl}/Data/stats");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;

                    var totalCards = root.TryGetProperty("totalCards", out var tc) ? tc.GetInt32() : 0;
                    var mode = root.TryGetProperty("persistenceMode", out var pm) ? pm.GetString() : "Unknown";
                    var creatures = root.TryGetProperty("creatures", out var cr) ? cr.GetInt32() : 0;
                    var withImages = root.TryGetProperty("withImages", out var wi) ? wi.GetInt32() : 0;

                    Console.WriteLine($"\nGeneral Statistics");
                    Console.WriteLine("──────────────────────────────────────────────────");
                    Console.WriteLine($"Total Cards:        {totalCards:N0}");
                    Console.WriteLine($"Persistence Mode:   {mode}");
                    Console.WriteLine($"Creatures:          {creatures:N0}");
                    Console.WriteLine($"With Images:        {withImages:N0}");

                    if (root.TryGetProperty("byRarity", out var byRarity) && byRarity.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"\nBy Rarity");
                        Console.WriteLine("──────────────────────────────────────────────────");

                        foreach (var item in byRarity.EnumerateArray())
                        {
                            var rarity = item.GetProperty("rarity").GetString();
                            var count = item.GetProperty("count").GetInt32();
                            var percentage = totalCards > 0 ? (count * 100.0 / totalCards) : 0;

                            Console.WriteLine($"  {rarity,-15} {count,7:N0}  ({percentage:F1}%)");
                        }
                    }

                    if (root.TryGetProperty("byType", out var byType) && byType.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"\nTop 10 Card Types");
                        Console.WriteLine("──────────────────────────────────────────────────");

                        int rank = 1;
                        foreach (var item in byType.EnumerateArray().Take(10))
                        {
                            var type = item.GetProperty("type").GetString();
                            var count = item.GetProperty("count").GetInt32();
                            var percentage = totalCards > 0 ? (count * 100.0 / totalCards) : 0;

                            Console.WriteLine($"  {rank,2}. {type,-25} {count,7:N0}  ({percentage:F1}%)");
                            rank++;
                        }
                    }

                    Console.WriteLine("──────────────────────────────────────────────────");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Unauthorized. Your session may have expired.");
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }





        // ==================== MCP ====================

        static async Task MCPQueryAsync()
        {
            Console.WriteLine("\nMCP - NATURAL LANGUAGE QUERY");
            Console.WriteLine(new string('─', 50));
            Console.WriteLine("Examples:");
            Console.WriteLine("  - cuantas cartas hay");
            Console.WriteLine("  - busca Lightning Bolt");
            Console.WriteLine("  - cartas azules");
            Console.WriteLine("  - criaturas raras");
            Console.WriteLine();
            Console.Write("Your query: ");

            var query = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                Console.WriteLine("Query cannot be empty");
                return;
            }

            var request = new { query };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                Console.WriteLine("\nProcessing with MCP routers...");

                var response = await _httpClient.PostAsync($"{_baseUrl}/mcp/query", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    var answer = GetPropertyOrDefault(result, "response");
                    var router = GetPropertyOrDefault(result, "routerUsed");
                    var resultCount = result.GetProperty("resultCount").GetInt32();
                    var executionTime = result.GetProperty("executionTimeMs").GetInt64();

                    Console.WriteLine($"\nResponse (via {router} Router):");
                    Console.WriteLine(new string('─', 50));
                    Console.WriteLine(answer);
                    Console.WriteLine(new string('─', 50));
                    Console.WriteLine($"Results: {resultCount} cards | Time: {executionTime}ms");

                    if (result.TryGetProperty("data", out var data) &&
                        data.ValueKind == JsonValueKind.Array &&
                        data.GetArrayLength() > 0)
                    {
                        Console.WriteLine("\nTop results:");
                        int count = 0;
                        foreach (var card in data.EnumerateArray())
                        {
                            var name = GetPropertyOrDefault(card, "name");
                            var type = GetPropertyOrDefault(card, "type");
                            Console.WriteLine($"  {count + 1}. {name} ({type})");
                            count++;
                            if (count >= 5) break;
                        }

                        if (data.GetArrayLength() > 5)
                        {
                            Console.WriteLine($"  ... and {data.GetArrayLength() - 5} more");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"\nError:");
                    Console.WriteLine($"   {responseJson}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        // ==================== HELPER METHODS ====================

        static string ReadPassword()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password.ToString();
        }

        static string GetPropertyOrDefault(JsonElement element, string propertyName, string defaultValue = "N/A")
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return defaultValue;

            if (prop.ValueKind == JsonValueKind.Null || prop.ValueKind == JsonValueKind.Undefined)
                return defaultValue;

            return prop.GetString() ?? defaultValue;
        }
    }
}
