using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Frontend.WPFApp.Models;

namespace Frontend.WPFApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _jwtToken = string.Empty;

        public ApiService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:53620"),
                Timeout = TimeSpan.FromMinutes(10)
            };
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var request = new LoginRequest { Username = username, Password = password };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/Auth/login", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

            _jwtToken = loginResponse.Token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);

            return loginResponse;
        }

        public async Task<GetAllResponse> GetAllCardsAsync()
        {
            var response = await _httpClient.GetAsync("/api/Data");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GetAllResponse>(content);
        }

        public async Task<Card> GetCardByIdAsync(string id)
        {
            var response = await _httpClient.GetAsync($"/api/Data/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Card>(content);
        }

        // ✅ CORREGIDO: Nombre correcto + captura de errores
        public async Task<bool> CreateCardAsync(Card card)
        {
            try
            {
                var json = JsonConvert.SerializeObject(card);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/Data", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error {response.StatusCode}: {errorContent}");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear carta: {ex.Message}", ex);
            }
        }

        // ✅ CORREGIDO: Captura de errores
        public async Task<bool> UpdateCardAsync(Card card)
        {
            try
            {
                var json = JsonConvert.SerializeObject(card);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"/api/Data/{card.Id}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error {response.StatusCode}: {errorContent}");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar carta: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteCardAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/api/Data/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<LoadKaggleResponse> LoadKaggleDataAsync()
        {
            var response = await _httpClient.PostAsync("/api/Data/load-kaggle", null);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<LoadKaggleResponse>(content);
        }

        public async Task<MCPQueryResponse> QueryMCPAsync(string query)
        {
            var json = JsonConvert.SerializeObject(new { Query = query });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/MCP/query", content); // ✅ CORREGIDO: eliminada 'c' extra
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<MCPQueryResponse>(responseContent);
        }

        public async Task<bool> SwitchPersistenceAsync(string mode)
        {
            var json = JsonConvert.SerializeObject(new { Mode = mode });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/Data/switch-persistence", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<(LoadKaggleResponse Memory, LoadKaggleResponse MySQL)> LoadToBothAsync()
        {
            var response = await _httpClient.PostAsync("/api/Data/load-to-both", null);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            dynamic result = JsonConvert.DeserializeObject<dynamic>(content);

            var memoryJson = result.memory.ToString();
            var mysqlJson = result.mysql.ToString();

            var memoryResult = JsonConvert.DeserializeObject<LoadKaggleResponse>(memoryJson);
            var mysqlResult = JsonConvert.DeserializeObject<LoadKaggleResponse>(mysqlJson);

            return (memoryResult, mysqlResult);
        }
    }
}
