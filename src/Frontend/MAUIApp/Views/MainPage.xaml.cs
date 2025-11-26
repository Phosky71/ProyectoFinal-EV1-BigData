using ProyectoFinal.Backend.MCP.Client;

namespace ProyectoFinal.Frontend.MAUIApp.Views;

/// <summary>
/// Pagina principal de la aplicacion MAUI.
/// Muestra el menu de navegacion y opciones principales.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly IMCPClient _mcpClient;
    private string _username = string.Empty;

    /// <summary>
    /// Constructor de MainPage.
    /// </summary>
    /// <param name="mcpClient">Cliente MCP para consultas</param>
    public MainPage(IMCPClient mcpClient)
    {
        InitializeComponent();
        _mcpClient = mcpClient;
        BindingContext = this;
    }

    /// <summary>
    /// Nombre del usuario autenticado.
    /// </summary>
    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Carga la informacion del usuario al aparecer.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Username = await SecureStorage.GetAsync("username") ?? "Usuario";
    }

    /// <summary>
    /// Navega a la vista Master/Detail.
    /// </summary>
    private async void OnMasterDetailClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MasterDetailPage));
    }

    /// <summary>
    /// Navega a la vista de interaccion MCP.
    /// </summary>
    private async void OnMCPInteractionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("mcp");
    }

    /// <summary>
    /// Navega a la configuracion.
    /// </summary>
    private async void OnConfigurationClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("config");
    }

    /// <summary>
    /// Cierra la sesion del usuario.
    /// </summary>
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Cerrar Sesion", 
            "Esta seguro que desea cerrar sesion?", "Si", "No");
        
        if (confirm)
        {
            SecureStorage.Remove("auth_token");
            SecureStorage.Remove("username");
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
