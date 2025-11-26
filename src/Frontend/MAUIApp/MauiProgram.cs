using Microsoft.Extensions.Logging;
using ProyectoFinal.Frontend.MAUIApp.Views;
using ProyectoFinal.Backend.MCP.Client;
using ProyectoFinal.Backend.Persistence;

namespace ProyectoFinal.Frontend.MAUIApp;

/// <summary>
/// Punto de entrada principal de la aplicacion MAUI.
/// Configura los servicios y la navegacion de la aplicacion.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Crea y configura la aplicacion MAUI.
    /// </summary>
    /// <returns>La aplicacion MAUI configurada</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Registrar servicios de persistencia
        RegisterPersistenceServices(builder.Services);

        // Registrar cliente MCP
        RegisterMCPServices(builder.Services);

        // Registrar ViewModels
        RegisterViewModels(builder.Services);

        // Registrar paginas para navegacion
        RegisterPages(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Registra los servicios de persistencia segun la configuracion.
    /// Implementa el patron Open/Close para cambio dinamico.
    /// </summary>
    private static void RegisterPersistenceServices(IServiceCollection services)
    {
        // TODO: Leer configuracion de App.config o appsettings.json
        // var persistenceType = ConfigurationManager.AppSettings["PersistenceSystem"];
        
        // Registro condicional basado en configuracion
        // if (persistenceType == "MySQL")
        //     services.AddSingleton(typeof(IRepository<>), typeof(MySQLRepository<>));
        // else
        //     services.AddSingleton(typeof(IRepository<>), typeof(MemoryRepository<>));
    }

    /// <summary>
    /// Registra los servicios del protocolo MCP.
    /// </summary>
    private static void RegisterMCPServices(IServiceCollection services)
    {
        // TODO: Registrar MCPClient y routers
        // services.AddSingleton<IMCPClient, MCPClient>();
    }

    /// <summary>
    /// Registra los ViewModels para inyeccion de dependencias.
    /// </summary>
    private static void RegisterViewModels(IServiceCollection services)
    {
        // TODO: Registrar ViewModels
        // services.AddTransient<LoginViewModel>();
        // services.AddTransient<MainViewModel>();
        // services.AddTransient<MasterDetailViewModel>();
    }

    /// <summary>
    /// Registra las paginas de la aplicacion.
    /// </summary>
    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<LoginPage>();
        services.AddTransient<MainPage>();
        services.AddTransient<MasterDetailPage>();
    }
}

/// <summary>
/// Clase principal de la aplicacion MAUI.
/// Gestiona el ciclo de vida y la navegacion.
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    /// <summary>
    /// Metodo llamado al iniciar la aplicacion.
    /// </summary>
    protected override void OnStart()
    {
        // TODO: Inicializar servicios
    }

    /// <summary>
    /// Metodo llamado cuando la aplicacion entra en segundo plano.
    /// </summary>
    protected override void OnSleep()
    {
        // TODO: Guardar estado
    }

    /// <summary>
    /// Metodo llamado cuando la aplicacion vuelve a primer plano.
    /// </summary>
    protected override void OnResume()
    {
        // TODO: Restaurar estado
    }
}
