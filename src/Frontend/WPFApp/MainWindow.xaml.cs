using System.Windows;

namespace Frontend.WPFApp
{
    public partial class MainWindow : Window
    {
        public static string JwtToken = "";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Show Login first
            var login = new LoginWindow();
            if (login.ShowDialog() != true)
            {
                Close();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenMaster_Click(object sender, RoutedEventArgs e)
        {
            var master = new MasterWindow();
            master.Show();
        }

        private void OpenMCP_Click(object sender, RoutedEventArgs e)
        {
            var mcp = new MCPWindow();
            mcp.Show();
        }

        private void OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Configuration Window Placeholder");
        }
    }
}
