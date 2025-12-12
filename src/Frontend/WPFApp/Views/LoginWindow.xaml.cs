using System;
using System.Windows;
using Frontend.WPFApp.Services;

namespace Frontend.WPFApp
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;

        public LoginWindow(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            txtPassword.Password = "admin123";
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = string.Empty;
            btnLogin.IsEnabled = false;

            try
            {
                var username = txtUsername.Text;
                var password = txtPassword.Password;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    txtError.Text = "Usuario y contraseña son obligatorios";
                    return;
                }

                var response = await _apiService.LoginAsync(username, password);

                MessageBox.Show($"Bienvenido {response.Username}!", "Login exitoso",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                txtError.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }
    }
}
