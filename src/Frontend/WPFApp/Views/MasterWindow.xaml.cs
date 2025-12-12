using System;
using System.Windows;
using Frontend.WPFApp.Models;
using Frontend.WPFApp.Services;

namespace Frontend.WPFApp
{
    public partial class MasterWindow : Window
    {
        private readonly ApiService _apiService;

        public MasterWindow(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            Loaded += async (s, e) => await LoadCardsAsync();
        }

        private async System.Threading.Tasks.Task LoadCardsAsync()
        {
            try
            {
                var response = await _apiService.GetAllCardsAsync();
                dgData.ItemsSource = response.Data;
                txtStatus.Text = $"Total: {response.Count} cartas | Modo: {response.PersistenceMode}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las cartas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadCardsAsync();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (dgData.SelectedItem is Card selectedCard)
            {
                var detailWindow = new DetailWindow(_apiService, selectedCard.Id);
                detailWindow.ShowDialog();
                _ = LoadCardsAsync();
            }
            else
            {
                MessageBox.Show("Selecciona una carta para editar", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (dgData.SelectedItem is Card selectedCard)
            {
                var result = MessageBox.Show($"¿Eliminar la carta '{selectedCard.Name}'?",
                    "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var success = await _apiService.DeleteCardAsync(selectedCard.Id);
                        if (success)
                        {
                            MessageBox.Show("Carta eliminada correctamente", "Exito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadCardsAsync();
                        }
                        else
                        {
                            MessageBox.Show("Error al eliminar la carta", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Selecciona una carta para eliminar", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
