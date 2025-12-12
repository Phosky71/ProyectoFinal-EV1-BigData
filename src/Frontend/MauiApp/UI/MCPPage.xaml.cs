using Microsoft.Maui;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MauiApp.UI;

public partial class MCPPage : ContentPage
{
    public MCPPage()
    {
        InitializeComponent();
    }

    private async void Send_Clicked(object sender, EventArgs e)
    {
        var query = txtInput.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        lblHistory.Text += $"\n\nYo: {query}";
        txtInput.Text = string.Empty;

        cvResults.IsVisible = false;
        cvResults.ItemsSource = null;

        using var client = new HttpClient();
        if (!string.IsNullOrEmpty(LoginPage.JwtToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginPage.JwtToken);

        var body = new { Query = query };
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        var url = DeviceInfo.Platform == DevicePlatform.Android
            ? "https://10.0.2.2:53620/api/MCP/query"
            : "https://localhost:53620/api/MCP/query";

        try
        {
            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                var jsonObj = JObject.Parse(json);

                string textResponse = jsonObj["response"]?.ToString() ?? "Sin respuesta de texto.";
                lblHistory.Text += $"\n\nMCP: {textResponse}";

                var dataToken = jsonObj.GetValue("data", StringComparison.OrdinalIgnoreCase);

                if (dataToken != null && dataToken.Type == JTokenType.Array && dataToken.HasValues)
                {
                    var cards = dataToken.ToObject<List<Card>>();

                    if (cards != null && cards.Count > 0)
                    {
                        cvResults.ItemsSource = cards;
                        cvResults.IsVisible = true; 
                    }
                }
            }
            else
            {
                lblHistory.Text += $"\n\nError HTTP: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            lblHistory.Text += $"\n\nError Excepción: {ex.Message}";
        }
    }
}

