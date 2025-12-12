using System;
using System.Linq;
using System.Windows;
using Frontend.WPFApp.Services;

namespace Frontend.WPFApp
{
    public partial class MCPWindow : Window
    {
        private readonly ApiService _apiService;

        public MCPWindow(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            var query = txtInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Escribe una consulta", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (query.Length > 500)
            {
                MessageBox.Show("La consulta es demasiado larga (maximo 500 caracteres)",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            txtHistory.AppendText($"Tu: {query}\n");
            txtInput.Clear();
            btnSend.IsEnabled = false;

            try
            {
                var response = await _apiService.QueryMCPAsync(query);

                txtHistory.AppendText($"MCP ({response.RouterUsed}): {response.Response}\n");
                txtHistory.AppendText($"  Resultados: {response.ResultCount} | Tiempo: {response.ExecutionTimeMs}ms\n\n");
                txtHistory.ScrollToEnd();

                if (response.Data != null && response.Data.Any())
                {
                    dgResults.ItemsSource = response.Data;
                    txtHistory.AppendText($"  Ver {response.Data.Count} cartas en la tabla de abajo\n\n");
                }
                else
                {
                    dgResults.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                txtHistory.AppendText($"Error: {ex.Message}\n\n");
                txtHistory.ScrollToEnd();
                dgResults.ItemsSource = null;
            }
            finally
            {
                btnSend.IsEnabled = true;
                txtInput.Focus();
            }
        }

        private void txtInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Send_Click(sender, e);
            }
        }
    }
}
