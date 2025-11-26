using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace Frontend.WPFApp
{
    public partial class MCPWindow : Window
    {
        public MCPWindow()
        {
            InitializeComponent();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendMessage();
        }

        private async void SendMessage()
        {
            var query = txtInput.Text;
            if (string.IsNullOrWhiteSpace(query)) return;

            txtHistory.AppendText($"You: {query}\n");
            txtInput.Clear();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MainWindow.JwtToken);
                var content = new StringContent(JsonConvert.SerializeObject(new { Query = query }), Encoding.UTF8, "application/json");
                
                try
                {
                    var response = await client.PostAsync("https://localhost:7001/api/MCP/query", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(json);
                        txtHistory.AppendText($"MCP: {data.response}\n\n");
                    }
                    else
                    {
                        txtHistory.AppendText("Error communicating with MCP.\n");
                    }
                }
                catch (Exception ex)
                {
                    txtHistory.AppendText($"Error: {ex.Message}\n");
                }
            }
        }
    }
}
