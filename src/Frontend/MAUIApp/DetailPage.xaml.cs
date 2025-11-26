using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;

namespace Frontend.MAUIApp
{
    public partial class DetailPage : ContentPage
    {
        public DetailPage()
        {
            InitializeComponent();
        }

        private async void Save_Clicked(object sender, EventArgs e)
        {
            var card = new { Name = txtName.Text, Type = txtType.Text, ManaCost = txtMana.Text };
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginPage.JwtToken);
                var content = new StringContent(JsonConvert.SerializeObject(card), Encoding.UTF8, "application/json");
                
                var url = DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:7001/api/Data" : "https://localhost:7001/api/Data";
                await client.PostAsync(url, content);
                await Navigation.PopAsync();
            }
        }
    }
}
