using System.Collections.ObjectModel;
using ProyectoFinal.Backend.Persistence;
using ProyectoFinal.Backend.MCP.Client;

namespace ProyectoFinal.Frontend.MAUIApp.Views;

/// <summary>
/// Pagina Master/Detail con implementacion de Lazy Loading.
/// Muestra una lista maestra con detalles bajo demanda.
/// </summary>
public partial class MasterDetailPage : ContentPage
{
    private readonly IRepository<IEntity> _repository;
    private readonly IMCPClient _mcpClient;
    private ObservableCollection<IEntity> _items;
    private IEntity _selectedItem;
    private bool _isLoading;
    private int _currentPage = 0;
    private const int PAGE_SIZE = 20;
    private bool _hasMoreItems = true;

    /// <summary>
    /// Constructor de MasterDetailPage.
    /// </summary>
    public MasterDetailPage(IRepository<IEntity> repository, IMCPClient mcpClient)
    {
        InitializeComponent();
        _repository = repository;
        _mcpClient = mcpClient;
        _items = new ObservableCollection<IEntity>();
        BindingContext = this;
    }

    /// <summary>
    /// Coleccion de items con lazy loading.
    /// </summary>
    public ObservableCollection<IEntity> Items
    {
        get => _items;
        set { _items = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Item seleccionado para mostrar detalle.
    /// </summary>
    public IEntity SelectedItem
    {
        get => _selectedItem;
        set { _selectedItem = value; OnPropertyChanged(); LoadDetailAsync(); }
    }

    /// <summary>
    /// Indica si esta cargando datos.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Carga inicial de datos con lazy loading.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_items.Count == 0)
            await LoadMoreItemsAsync();
    }

    /// <summary>
    /// Implementa lazy loading para cargar mas items.
    /// Se llama cuando el usuario hace scroll al final de la lista.
    /// </summary>
    public async Task LoadMoreItemsAsync()
    {
        if (IsLoading || !_hasMoreItems) return;

        try
        {
            IsLoading = true;
            var items = await _repository.GetAllAsync();
            var pagedItems = items.Skip(_currentPage * PAGE_SIZE).Take(PAGE_SIZE).ToList();

            if (pagedItems.Count < PAGE_SIZE)
                _hasMoreItems = false;

            foreach (var item in pagedItems)
                _items.Add(item);

            _currentPage++;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error cargando datos: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Carga los detalles del item seleccionado.
    /// </summary>
    private async void LoadDetailAsync()
    {
        if (_selectedItem == null) return;
        // TODO: Cargar detalles adicionales del item
    }

    /// <summary>
    /// Evento cuando se alcanza el final del scroll (lazy loading trigger).
    /// </summary>
    private async void OnRemainingItemsThresholdReached(object sender, EventArgs e)
    {
        await LoadMoreItemsAsync();
    }

    /// <summary>
    /// Ejecuta una consulta MCP sobre los datos.
    /// </summary>
    private async void OnMCPQueryClicked(object sender, EventArgs e)
    {
        string query = await DisplayPromptAsync("Consulta MCP", "Ingrese su consulta:");
        if (!string.IsNullOrWhiteSpace(query))
        {
            var result = await _mcpClient.ProcessQueryAsync(query);
            await DisplayAlert("Resultado MCP", result ?? "Sin resultados", "OK");
        }
    }
}
