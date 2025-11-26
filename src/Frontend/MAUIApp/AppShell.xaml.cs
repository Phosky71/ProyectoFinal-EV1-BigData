using ProyectoFinal.Frontend.MAUIApp.Views;

namespace ProyectoFinal.Frontend.MAUIApp;

/// <summary>
/// Shell de navegacion principal de la aplicacion MAUI.
/// Define las rutas de navegacion y la estructura de la aplicacion.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// Constructor de AppShell.
    /// Configura las rutas de navegacion.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    /// <summary>
    /// Registra las rutas de navegacion de la aplicacion.
    /// Permite navegacion programatica entre paginas.
    /// </summary>
    private void RegisterRoutes()
    {
        // Rutas principales
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(MasterDetailPage), typeof(MasterDetailPage));
        
        // Rutas para funcionalidades especificas
        Routing.RegisterRoute("mcp", typeof(MCPInteractionPage));
        Routing.RegisterRoute("config", typeof(ConfigurationPage));
    }

    /// <summary>
    /// Navega a la pagina especificada.
    /// </summary>
    /// <param name="route">Ruta de navegacion</param>
    public async Task NavigateToAsync(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    /// <summary>
    /// Navega a la pagina especificada con parametros.
    /// </summary>
    /// <param name="route">Ruta de navegacion</param>
    /// <param name="parameters">Parametros de navegacion</param>
    public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
    {
        await Shell.Current.GoToAsync(route, parameters);
    }

    /// <summary>
    /// Navega hacia atras en la pila de navegacion.
    /// </summary>
    public async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Navega a la raiz de la navegacion.
    /// </summary>
    public async Task GoToRootAsync()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}

// Placeholder classes - crear archivos separados
public partial class MCPInteractionPage : ContentPage { }
public partial class ConfigurationPage : ContentPage { }
