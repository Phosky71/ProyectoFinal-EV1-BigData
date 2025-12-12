using System;
using System.Windows;
using Frontend.WPFApp.Services;

namespace Frontend.WPFApp
{
    public partial class ConfigurationWindow : Window
    {
        private readonly ApiService _apiService;

        public ConfigurationWindow(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            Loaded += ConfigurationWindow_Loaded;
        }

        private async void ConfigurationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCurrentPersistenceModeAsync();
        }

        private async System.Threading.Tasks.Task LoadCurrentPersistenceModeAsync()
        {
            try
            {
                var response = await _apiService.GetAllCardsAsync();
                var currentMode = response.PersistenceMode;

                txtCurrentMode.Text = $"Modo actual: {currentMode}";

                if (currentMode == "Memory")
                {
                    rbMemory.IsChecked = true;
                }
                else if (currentMode == "MySQL")
                {
                    rbMySQL.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el modo de persistencia: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============ IMPORTAR DATOS ============

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Esto cargara los datos del CSV de Kaggle a MEMORIA y MYSQL.\n\n" +
                "ADVERTENCIA: Los datos existentes seran actualizados.\n\n" +
                "¿Continuar?",
                "Confirmar importacion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            txtStatus.Text = "Importando datos a MEMORIA y MYSQL...\nPor favor espere, puede tardar varios minutos...";
            txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
            btnImport.IsEnabled = false;

            try
            {
                var bothResults = await _apiService.LoadToBothAsync();

                var memoryResult = bothResults.Memory;
                var mysqlResult = bothResults.MySQL;

                if (memoryResult.Success && mysqlResult.Success)
                {
                    txtStatus.Text = "IMPORTACION EXITOSA\n\n" +
                                    $"Memoria: {memoryResult.RecordsLoaded:N0} cartas cargadas\n" +
                                    $"MySQL: {mysqlResult.RecordsLoaded:N0} cartas cargadas\n\n" +
                                    "Ambos sistemas estan sincronizados.";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;

                    MessageBox.Show(
                        $"Datos importados correctamente:\n\n" +
                        $"- Memoria: {memoryResult.RecordsLoaded:N0} cartas\n" +
                        $"- MySQL: {mysqlResult.RecordsLoaded:N0} cartas",
                        "Importacion completada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    var errors = "ERRORES DURANTE LA IMPORTACION:\n\n";

                    if (!memoryResult.Success)
                        errors += $"Memoria: {memoryResult.Error}\n";
                    else
                        errors += $"Memoria: OK ({memoryResult.RecordsLoaded:N0} cartas)\n";

                    if (!mysqlResult.Success)
                        errors += $"MySQL: {mysqlResult.Error}\n";
                    else
                        errors += $"MySQL: OK ({mysqlResult.RecordsLoaded:N0} cartas)\n";

                    txtStatus.Text = errors;
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;

                    MessageBox.Show(errors, "Error en importacion",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"ERROR:\n{ex.Message}";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;

                MessageBox.Show($"Error durante la importacion:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnImport.IsEnabled = true;
            }
        }

        // ============ CAMBIAR PERSISTENCIA ============

        private async void Persistence_Changed(object sender, RoutedEventArgs e)
        {
            if (rbMemory.IsChecked == true || rbMySQL.IsChecked == true)
            {
                await SwitchPersistenceAsync();
            }
        }

        private async System.Threading.Tasks.Task SwitchPersistenceAsync()
        {
            try
            {
                string newMode = rbMemory.IsChecked == true ? "Memory" : "MySQL";

                var success = await _apiService.SwitchPersistenceAsync(newMode);

                if (success)
                {
                    txtCurrentMode.Text = $"Modo actual: {newMode}";
                    txtStatus.Text = $"Sistema cambiado a {newMode} correctamente";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;

                    MessageBox.Show(
                        $"El sistema ahora usa {newMode} para las operaciones CRUD.",
                        "Cambio exitoso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    txtStatus.Text = "Error al cambiar el sistema de persistencia";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    await LoadCurrentPersistenceModeAsync();
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error: {ex.Message}";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                await LoadCurrentPersistenceModeAsync();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
