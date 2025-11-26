using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using Newtonsoft.Json;

namespace Frontend.WPFApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUsername.Text;
            var password = txtPassword.Password;

            // Call API
            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
                try 
                {
                    var response = await client.PostAsync("https://localhost:7001/api/Auth/login", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(json);
                        MainWindow.JwtToken = data.token;
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Login Failed");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }
    }
}
