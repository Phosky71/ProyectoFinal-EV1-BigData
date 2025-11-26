using ProyectoFinal.Backend.API.Auth;

namespace ProyectoFinal.Frontend.MAUIApp.Views;

/// <summary>
/// Pagina de inicio de sesion de la aplicacion MAUI.
/// Permite a los usuarios autenticarse con credenciales.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly IJwtService _jwtService;
    private string _username = string.Empty;
    private string _password = string.Empty;

    /// <summary>
    /// Constructor de LoginPage.
    /// </summary>
    /// <param name="jwtService">Servicio de autenticacion JWT</param>
    public LoginPage(IJwtService jwtService)
    {
        InitializeComponent();
        _jwtService = jwtService;
        BindingContext = this;
    }

    /// <summary>
    /// Nombre de usuario ingresado.
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
    /// Contrasena ingresada.
    /// </summary>
    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Maneja el evento de clic en el boton de login.
    /// </summary>
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await DisplayAlert("Error", "Por favor ingrese usuario y contrasena", "OK");
            return;
        }

        try
        {
            // TODO: Validar credenciales contra el backend
            var token = _jwtService.GenerateToken(Username);
            
            if (!string.IsNullOrEmpty(token))
            {
                // Guardar token en preferencias
                await SecureStorage.SetAsync("auth_token", token);
                await SecureStorage.SetAsync("username", Username);
                
                // Navegar a la pagina principal
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await DisplayAlert("Error", "Credenciales invalidas", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al iniciar sesion: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Verifica si existe una sesion activa al cargar la pagina.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            var token = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token) && _jwtService.ValidateToken(token))
            {
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
        catch
        {
            // Token no existe o es invalido, mostrar login
        }
    }
}
