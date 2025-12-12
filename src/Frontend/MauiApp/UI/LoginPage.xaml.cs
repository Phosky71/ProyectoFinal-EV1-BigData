using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace MauiApp.UI;

public partial class LoginPage : ContentPage
{
    public static string JwtToken = string.Empty;

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void Login_Clicked(object sender, EventArgs e)
    {
        var username = txtUsername.Text;
        var password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Usuario y contraseña son obligatorios", "OK");
            return;
        }

        using var client = new HttpClient();
        var body = new { Username = username, Password = password };
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        var url = DeviceInfo.Platform == DevicePlatform.Android
            ? "https://10.0.2.2:53620/api/Auth/login"
            : "https://localhost:53620/api/Auth/login";

        try
        {
            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(json);
                JwtToken = data.token;

                await Shell.Current.GoToAsync("//Main");
            }
            else
            {
                await DisplayAlert("Error", "Login incorrecto", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
