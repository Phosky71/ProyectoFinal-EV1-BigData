using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using Newtonsoft.Json;

namespace Frontend.WPFApp
{
    public partial class MasterWindow : Window
    {
        public MasterWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private async void LoadData()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MainWindow.JwtToken);
                try
                {
                    var response = await client.GetAsync("https://localhost:7001/api/Data");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<List<dynamic>>(json);
                        dgData.ItemsSource = data;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (dgData.SelectedItem == null) return;
            dynamic item = dgData.SelectedItem;
            string id = item.id; // Case sensitive, check JSON

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MainWindow.JwtToken);
                var response = await client.DeleteAsync($"https://localhost:7001/api/Data/{id}");
                if (response.IsSuccessStatusCode) LoadData();
                else MessageBox.Show("Delete failed");
            }
        }
    }
}
