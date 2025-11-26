using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using Newtonsoft.Json;

namespace Frontend.WPFApp
{
    public partial class DetailWindow : Window
    {
        public DetailWindow()
        {
            InitializeComponent();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var card = new { Name = txtName.Text, Type = txtType.Text, ManaCost = txtMana.Text };
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MainWindow.JwtToken);
                var content = new StringContent(JsonConvert.SerializeObject(card), Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync("https://localhost:7001/api/Data", content);
                if (response.IsSuccessStatusCode) Close();
                else MessageBox.Show("Error saving");
            }
        }
    }
}
