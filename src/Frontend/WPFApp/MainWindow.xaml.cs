using System;
using System.Windows;
using System.Windows.Controls;

namespace ProyectoFinal.Frontend.WPFApp
{
    /// <summary>
    /// Ventana principal de la aplicacion WPF con interfaz MDI.
    /// Funcionalidades:
    /// - Login de usuario
    /// - Configuracion del sistema
    /// - Pantallas Master/Detail
    /// - Interaccion con MCP
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isAuthenticated;
        private string _currentUser;

        public MainWindow()
        {
            InitializeComponent();
            _isAuthenticated = false;
            _currentUser = string.Empty;
            
            // Mostrar login al iniciar
            ShowLoginDialog();
        }

        #region Authentication

        private void ShowLoginDialog()
        {
            // TODO: Implementar dialogo de login
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                _isAuthenticated = true;
                _currentUser = loginWindow.Username;
                UpdateUI();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region MDI Management

        /// <summary>
        /// Abre una nueva ventana hija en el contenedor MDI.
        /// </summary>
        private void OpenMdiChild(UserControl content, string title)
        {
            var tabItem = new TabItem
            {
                Header = title,
                Content = content
            };
            
            // TODO: Agregar al TabControl MDI
            // MdiContainer.Items.Add(tabItem);
            // MdiContainer.SelectedItem = tabItem;
        }

        /// <summary>
        /// Cierra la ventana hija activa.
        /// </summary>
        private void CloseMdiChild()
        {
            // TODO: Cerrar tab activo
        }

        #endregion

        #region Menu Commands

        private void MenuItem_Config_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Abrir ventana de configuracion
            OpenMdiChild(new ConfigView(), "Configuracion");
        }

        private void MenuItem_MasterDetail_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Abrir vista Master/Detail
            OpenMdiChild(new MasterDetailView(), "Datos");
        }

        private void MenuItem_MCP_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Abrir interaccion con MCP
            OpenMdiChild(new MCPInteractionView(), "Consultas MCP");
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region UI Updates

        private void UpdateUI()
        {
            Title = $"Proyecto Final EV1 - {_currentUser}";
            // TODO: Actualizar elementos de la UI segun estado
        }

        #endregion
    }

    // Placeholder classes - crear archivos separados
    public class LoginWindow : Window
    {
        public string Username { get; set; } = "";
    }

    public class ConfigView : UserControl { }
    public class MasterDetailView : UserControl { }
    public class MCPInteractionView : UserControl { }
}
