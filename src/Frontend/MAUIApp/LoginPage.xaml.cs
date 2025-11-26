using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;

namespace Frontend.MAUIApp
{
    public partial class LoginPage : ContentPage
    {
        public static string JwtToken = "";

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void Login_Clicked(object sender, EventArgs e)
        {
            var username = txtUsername.Text;
            var password = txtPassword.Text;

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
                try
                {
                    // Use 10.0.2.2 for Android emulator to access localhost
                    var url = DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:7001/api/Auth/login" : "https://localhost:7001/api/Auth/login";
                    
                    // Note: SSL certificate validation might fail on emulator. In real app, handle this.
                    // For this snippet, assuming HttpClientHandler is configured in MauiProgram.cs to bypass SSL for dev.
                    
                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(json);
                        JwtToken = data.token;
                        await Shell.Current.GoToAsync("//MasterPage");
                    }
                    else
                    {
                        await DisplayAlert("Error", "Login Failed", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }
    }
}
