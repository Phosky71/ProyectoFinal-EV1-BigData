using System.Windows;
using Frontend.WPFApp.Services;

namespace Frontend.WPFApp
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            ShowLoginAndInitialize();
        }

        private void ShowLoginAndInitialize()
        {
            var loginWindow = new LoginWindow(_apiService);
            var result = loginWindow.ShowDialog();

            if (result != true)
            {
                Close();
            }
        }

        private void MenuConfiguration_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new ConfigurationWindow(_apiService);
            configWindow.ShowDialog();
        }

        private void MenuMaster_Click(object sender, RoutedEventArgs e)
        {
            var masterWindow = new MasterWindow(_apiService);
            masterWindow.Show();
        }

        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            var detailWindow = new DetailWindow(_apiService, null);
            detailWindow.ShowDialog();
        }

        private void MenuMCP_Click(object sender, RoutedEventArgs e)
        {
            var mcpWindow = new MCPWindow(_apiService);
            mcpWindow.Show();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
