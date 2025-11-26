using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Frontend.ConsoleApp
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        static string _jwtToken = "";
        static string _baseUrl = "https://localhost:7001/api"; // Adjust port as needed

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Proyecto Final EV1 - Console Client ===");
            
            // 1. Login
            if (!await Login()) return;

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\nMenu:");
                Console.WriteLine("1. List Cards");
                Console.WriteLine("2. Find Card by ID");
                Console.WriteLine("3. Create Card");
                Console.WriteLine("4. Delete Card");
                Console.WriteLine("5. Switch Persistence");
                Console.WriteLine("6. Load Data from Kaggle (Simulated)");
                Console.WriteLine("7. Exit");
                Console.Write("Select option: ");
                
                switch (Console.ReadLine())
                {
                    case "1": await ListCards(); break;
                    case "2": await FindCard(); break;
                    case "3": await CreateCard(); break;
                    case "4": await DeleteCard(); break;
                    case "5": await SwitchPersistence(); break;
                    case "6": await LoadData(); break;
                    case "7": exit = true; break;
                }
            }
        }

        static async Task<bool> Login()
        {
            Console.Write("Username (admin): ");
            string username = Console.ReadLine();
            Console.Write("Password (password): ");
            string password = Console.ReadLine();

            var loginModel = new { Username = username, Password = password };
            var content = new StringContent(JsonConvert.SerializeObject(loginModel), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{_baseUrl}/Auth/login", content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(json);
                    _jwtToken = data.token;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
                    Console.WriteLine("Login Successful!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
            }
            
            Console.WriteLine("Login Failed.");
            return false;
        }

        static async Task ListCards()
        {
            var response = await client.GetAsync($"{_baseUrl}/Data");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var cards = JsonConvert.DeserializeObject<List<dynamic>>(json);
                Console.WriteLine($"\nTotal Cards: {cards.Count}");
                foreach (var card in cards)
                {
                    Console.WriteLine($"- {card.name} ({card.type})");
                }
            }
            else Console.WriteLine("Error fetching data.");
        }

        static async Task FindCard()
        {
            Console.Write("Enter ID: ");
            string id = Console.ReadLine();
            var response = await client.GetAsync($"{_baseUrl}/Data/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                dynamic card = JsonConvert.DeserializeObject(json);
                Console.WriteLine($"Found: {card.name}");
            }
            else Console.WriteLine("Not found.");
        }

        static async Task CreateCard()
        {
            var card = new
            {
                Name = "New Card",
                Type = "Creature",
                ManaCost = "{W}",
                Rarity = "Common"
            };
            var content = new StringContent(JsonConvert.SerializeObject(card), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_baseUrl}/Data", content);
            Console.WriteLine(response.IsSuccessStatusCode ? "Created!" : "Error.");
        }

        static async Task DeleteCard()
        {
            Console.Write("Enter ID: ");
            string id = Console.ReadLine();
            var response = await client.DeleteAsync($"{_baseUrl}/Data/{id}");
            Console.WriteLine(response.IsSuccessStatusCode ? "Deleted!" : "Error.");
        }

        static async Task SwitchPersistence()
        {
            Console.WriteLine("1. Memory");
            Console.WriteLine("2. MySQL");
            string choice = Console.ReadLine();
            string type = choice == "2" ? "MySQL" : "Memory";
            
            var model = new { Type = type };
            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_baseUrl}/Config/persistence", content);
            Console.WriteLine(response.IsSuccessStatusCode ? $"Switched to {type}" : "Error.");
        }

        static async Task LoadData()
        {
            // In a real app, this might trigger a backend job.
            // Here we just simulate or call a load endpoint if we had one.
            Console.WriteLine("Requesting backend to load data...");
            // For now, we assume the backend loads on startup or we could add an endpoint.
            Console.WriteLine("Data load triggered (Simulation).");
            await Task.CompletedTask;
        }
    }
}
