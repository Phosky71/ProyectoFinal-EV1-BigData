using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;

namespace Frontend.MAUIApp
{
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

            txtHistory.Text += $"You: {query}\n";
            txtInput.Text = "";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginPage.JwtToken);
                var content = new StringContent(JsonConvert.SerializeObject(new { Query = query }), Encoding.UTF8, "application/json");
                
                try
                {
                    var url = DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:7001/api/MCP/query" : "https://localhost:7001/api/MCP/query";
                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(json);
                        txtHistory.Text += $"MCP: {data.response}\n\n";
                    }
                }
                catch (Exception ex)
                {
                    txtHistory.Text += $"Error: {ex.Message}\n";
                }
            }
        }
    }
}
