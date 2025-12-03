using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Frontend.ConsoleApp
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string? _jwtToken;
        private static readonly string _baseUrl = "https://localhost:7001/api"; // Ajustar según tu puerto

        static async Task Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine("║  Proyecto Final EV1 - Console Client      ║");
            Console.WriteLine("║  Magic: The Gathering Card Manager        ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();

            // Ignorar certificados SSL en desarrollo (solo para testing)
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // 1. Login (obligatorio)
            if (!await LoginAsync())
            {
                Console.WriteLine("\n❌ Login failed. Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // 2. Menú principal
            await ShowMainMenuAsync();
        }

        static async Task ShowMainMenuAsync()
        {
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("\n" + new string('═', 50));
                Console.WriteLine("MAIN MENU");
                Console.WriteLine(new string('═', 50));
                Console.WriteLine("1. 📋 List All Cards");
                Console.WriteLine("2. 🔍 Find Card by ID");
                Console.WriteLine("3. ➕ Create New Card");
                Console.WriteLine("4. ✏️  Update Card");
                Console.WriteLine("5. 🗑️  Delete Card");
                Console.WriteLine("6. ⚙️  Switch Persistence Mode");
                Console.WriteLine("7. 📥 Load Data from Kaggle CSV");
                Console.WriteLine("8. 📊 View Statistics");
                Console.WriteLine("9. 🤖 MCP Query (Natural Language)");
                Console.WriteLine("0. ❌ Exit");
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
                            Console.WriteLine("\n⚠️  Invalid option. Try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Error: {ex.Message}");
                }

                if (!exit)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        // ==================== LOGIN ====================

        static async Task<bool> LoginAsync()
        {
            Console.WriteLine("\nLOGIN");
            Console.WriteLine(new string('─', 50));

            Console.Write("Username (default: admin): ");
            string username = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(username)) username = "admin";

            Console.Write("Password (default: admin123): ");
            string password = ReadPassword();
            if (string.IsNullOrEmpty(password)) password = "admin123";

            var loginRequest = new { Username = username, Password = password };
            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/Auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

                    _jwtToken = loginResponse.GetProperty("token").GetString();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _jwtToken);

                    var user = loginResponse.GetProperty("username").GetString();
                    var role = loginResponse.GetProperty("role").GetString();

                    Console.WriteLine($"\n✅ Login successful!");
                    Console.WriteLine($"   User: {user}");
                    Console.WriteLine($"   Role: {role}");

                    return true;
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"\n❌ Login failed: {response.StatusCode}");
                    Console.WriteLine($"   {errorJson}");
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"\n❌ Connection error: {ex.Message}");
                Console.WriteLine($"   Make sure the API is running at {_baseUrl}");
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
                var response = await _httpClient.GetAsync($"{_baseUrl}/Data");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(json);
                    var count = result.GetProperty("count").GetInt32();
                    var data = result.GetProperty("data");

                    Console.WriteLine($"\n✅ Total cards: {count}\n");

                    int index = 1;
                    foreach (var card in data.EnumerateArray())
                    {
                        var name = card.GetProperty("name").GetString();
                        var type = card.TryGetProperty("type", out var t) ? t.GetString() : "N/A";
                        var rarity = card.TryGetProperty("rarity", out var r) ? r.GetString() : "N/A";

                        Console.WriteLine($"{index,3}. {name} - {type} ({rarity})");
                        index++;

                        if (index > 20) // Limitar a 20 para no saturar consola
                        {
                            Console.WriteLine($"\n... and {count - 20} more cards");
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static async Task FindCardAsync()
        {
            Console.Write("\nEnter Card ID: ");
            string? id = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine("❌ ID cannot be empty");
                return;
            }

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Data/{id}");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var card = JsonSerializer.Deserialize<JsonElement>(json);

                    Console.WriteLine("\n✅ Card found:");
                    Console.WriteLine($"   ID: {card.GetProperty("id").GetString()}");
                    Console.WriteLine($"   Name: {card.GetProperty("name").GetString()}");
                    Console.WriteLine($"   Type: {GetPropertyOrDefault(card, "type")}");
                    Console.WriteLine($"   Mana Cost: {GetPropertyOrDefault(card, "manaCost")}");
                    Console.WriteLine($"   Rarity: {GetPropertyOrDefault(card, "rarity")}");
                    Console.WriteLine($"   Set: {GetPropertyOrDefault(card, "setName")}");

                    if (card.TryGetProperty("power", out var power) &&
                        card.TryGetProperty("toughness", out var toughness))
                    {
                        Console.WriteLine($"   P/T: {power.GetString()}/{toughness.GetString()}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Card not found (ID: {id})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static async Task CreateCardAsync()
        {
            Console.WriteLine("\n➕ CREATE NEW CARD");
            Console.WriteLine(new string('─', 50));

            Console.Write("Name: ");
            var name = Console.ReadLine()?.Trim();

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
                Name = name,
                Type = type,
                ManaCost = manaCost,
                Rarity = rarity,
                SetName = setName
            };

            try
            {
                var json = JsonSerializer.Serialize(card);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/Data", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var created = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    var id = created.GetProperty("id").GetString();

                    Console.WriteLine($"\n✅ Card created successfully!");
                    Console.WriteLine($"   ID: {id}");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"\n❌ Error creating card: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static async Task UpdateCardAsync()
        {
            Console.Write("\nEnter Card ID to update: ");
            string? id = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine("❌ ID cannot be empty");
                return;
            }

            // Primero obtener la carta actual
            var getResponse = await _httpClient.GetAsync($"{_baseUrl}/Data/{id}");
            if (!getResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Card not found (ID: {id})");
                return;
            }

            var existingJson = await getResponse.Content.ReadAsStringAsync();
            var existing = JsonSerializer.Deserialize<JsonElement>(existingJson);

            Console.WriteLine("\n📝 Current values (press Enter to keep):");

            Console.Write($"Name [{existing.GetProperty("name").GetString()}]: ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) name = existing.GetProperty("name").GetString();

            // ... similar para otros campos

            var updated = new
            {
                Id = id,
                Name = name,
                Type = GetPropertyOrDefault(existing, "type"),
                ManaCost = GetPropertyOrDefault(existing, "manaCost"),
                Rarity = GetPropertyOrDefault(existing, "rarity"),
                SetName = GetPropertyOrDefault(existing, "setName")
            };

            try
            {
                var json = JsonSerializer.Serialize(updated);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}/Data/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("\n✅ Card updated successfully!");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"\n❌ Error: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static async Task DeleteCardAsync()
        {
            Console.Write("\n🗑️  Enter Card ID to delete: ");
            string? id = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine("❌ ID cannot be empty");
                return;
            }

            Console.Write($"⚠️  Are you sure you want to delete card {id}? (y/N): ");
            var confirm = Console.ReadLine()?.Trim().ToLower();

            if (confirm != "y" && confirm != "yes")
            {
                Console.WriteLine("❌ Deletion cancelled");
                return;
            }

            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/Data/{id}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("\n✅ Card deleted successfully!");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"\n❌ Error: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        // ==================== CONFIGURATION ====================

        static async Task SwitchPersistenceAsync()
        {
            Console.WriteLine("\nSWITCH PERSISTENCE MODE");
            Console.WriteLine(new string('─', 50));
            Console.WriteLine("1. Memory (LINQ)");
            Console.WriteLine("2. MySQL");
            Console.Write("\nSelect mode: ");

            string? choice = Console.ReadLine()?.Trim();
            string mode = choice == "2" ? "MySQL" : "Memory";

            var request = new { Mode = mode };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/Config/persistence", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"\n✅ Persistence switched to {mode}");
                    Console.WriteLine($"   {responseJson}");
                }
                else
                {
                    Console.WriteLine($"\n❌ Error: {responseJson}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        // ==================== KAGGLE DATA LOADING ====================

        static async Task LoadDataFromKaggleAsync()
        {
            Console.WriteLine("\nLOAD DATA FROM KAGGLE CSV");
            Console.WriteLine(new string('─', 50));
            Console.WriteLine("⚠️  This will load cards from the Kaggle dataset");
            Console.WriteLine("   File: Data/cards.csv (configured in API)");
            Console.Write("\nProceed? (y/N): ");

            var confirm = Console.ReadLine()?.Trim().ToLower();
            if (confirm != "y" && confirm != "yes")
            {
                Console.WriteLine("❌ Operation cancelled");
                return;
            }

            try
            {
                Console.WriteLine("\n⏳ Loading data... This may take a while...");

                var response = await _httpClient.PostAsync($"{_baseUrl}/Data/load-kaggle", null);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(json);
                    var recordsLoaded = result.GetProperty("recordsLoaded").GetInt32();
                    var persistenceMode = result.GetProperty("persistenceMode").GetString();

                    Console.WriteLine("\n✅ Data loaded successfully!");
                    Console.WriteLine($"   Records loaded: {recordsLoaded}");
                    Console.WriteLine($"   Persistence mode: {persistenceMode}");
                }
                else
                {
                    Console.WriteLine($"\n❌ Error loading data:");
                    Console.WriteLine($"   {json}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static async Task ViewStatisticsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Data/stats");
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var stats = JsonSerializer.Deserialize<JsonElement>(json);
                    var total = stats.GetProperty("totalRecords").GetInt32();
                    var mode = stats.GetProperty("persistenceMode").GetString();

                    Console.WriteLine("\nSTATISTICS");
                    Console.WriteLine(new string('─', 50));
                    Console.WriteLine($"Total Records: {total}");
                    Console.WriteLine($"Persistence Mode: {mode}");
                }
                else
                {
                    Console.WriteLine($"❌ Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        // ==================== MCP (BONUS) ====================

        static async Task MCPQueryAsync()
        {
            Console.WriteLine("\nMCP - NATURAL LANGUAGE QUERY");
            Console.WriteLine(new string('─', 50));
            Console.WriteLine("Examples:");
            Console.WriteLine("  - How many cards are there?");
            Console.WriteLine("  - Find blue creatures");
            Console.WriteLine("  - Show rare cards");
            Console.WriteLine();
            Console.Write("Your query: ");

            var query = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                Console.WriteLine("❌ Query cannot be empty");
                return;
            }

            var request = new { Query = query };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                Console.WriteLine("\n⏳ Processing...");

                var response = await _httpClient.PostAsync($"{_baseUrl}/MCP/query", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    var answer = result.GetProperty("response").GetString();
                    var router = result.GetProperty("router").GetString();
                    var resultCount = result.GetProperty("resultCount").GetInt32();

                    Console.WriteLine($"\n✅ Response (via {router} router):");
                    Console.WriteLine($"   {answer}");
                    Console.WriteLine($"\n   Results: {resultCount} cards");
                }
                else
                {
                    Console.WriteLine($"\n❌ Error: {responseJson}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
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
            return element.TryGetProperty(propertyName, out var prop) ?
                   (prop.ValueKind == JsonValueKind.Null ? defaultValue : prop.GetString() ?? defaultValue) :
                   defaultValue;
        }
    }
}
