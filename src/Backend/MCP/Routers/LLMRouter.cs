using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json; // Assuming Newtonsoft.Json is available or using System.Text.Json

namespace Backend.MCP.Routers
{
    public class LLMRouter : IMCPRouter
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public LLMRouter()
        {
            _apiKey = ConfigurationManager.AppSettings["OpenAIKey"];
            _httpClient = new HttpClient();
        }

        public bool CanHandle(string query)
        {
            return true; // Fallback for everything
        }

        public async Task<string> ProcessRequestAsync(string query)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_OPENAI_API_KEY_HERE")
            {
                return "LLM API Key not configured. Unable to process complex query.";
            }

            // Simple OpenAI Chat Completion call
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are an assistant for a Magic: The Gathering card database. Answer the user's query about cards." },
                    new { role = "user", content = query }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic json = JsonConvert.DeserializeObject(responseString);
                    return json.choices[0].message.content;
                }
                return "Error calling LLM provider: " + response.StatusCode;
            }
            catch (System.Exception ex)
            {
                return "Exception calling LLM: " + ex.Message;
            }
        }
    }
}
