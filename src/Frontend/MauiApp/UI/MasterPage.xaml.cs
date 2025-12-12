using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace MauiApp.UI;

public partial class MasterPage : ContentPage
{
    public ObservableRangeCollection<Card> Cards { get; set; } = new();
    private List<Card> _allData = new();
    private bool _isLoading = false;
    private int _page = 0;
    private const int PageSize = 20;

    public MasterPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAllDataAsync();
    }

    private async Task LoadAllDataAsync()
    {
        if (_isLoading) return;
        _isLoading = true;

        using var client = new HttpClient();
        if (!string.IsNullOrEmpty(LoginPage.JwtToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginPage.JwtToken);
        }

        var url = DeviceInfo.Platform == DevicePlatform.Android
            ? "https://10.0.2.2:53620/api/Data"
            : "https://localhost:53620/api/Data";

        try
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();


                Console.WriteLine($"JSON RECIBIDO: {json.Substring(0, Math.Min(json.Length, 200))}...");

   
                try
                {
                    var listaDirecta = JsonConvert.DeserializeObject<List<Card>>(json);
                    if (listaDirecta != null && listaDirecta.Count > 0)
                    {
                        _allData = listaDirecta;
                        goto FinalizarCarga; 
                    }
                }
                catch { }


                var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);

                var arrayToken = jObj.GetValue("data", StringComparison.OrdinalIgnoreCase)
                              ?? jObj.GetValue("items", StringComparison.OrdinalIgnoreCase)
                              ?? jObj.GetValue("value", StringComparison.OrdinalIgnoreCase)
                              ?? jObj.GetValue("result", StringComparison.OrdinalIgnoreCase);

                if (arrayToken != null)
                {
                    _allData = arrayToken.ToObject<List<Card>>() ?? new List<Card>();
                }
                else
                {
                    await DisplayAlert("Error JSON", "No encuentro la lista de cartas en la respuesta del servidor.", "OK");
                    _allData = new List<Card>();
                }

            FinalizarCarga:

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_allData.Count > 0)
                    {
                        Cards.Clear(); 
                        _page = 0;     
                        LoadNextPage(); 
                    }
                    else
                    {
                        DisplayAlert("Vacío", "El servidor devolvió 0 cartas.", "OK");
                    }
                });
            }
            else
            {
                await DisplayAlert("Error", $"Error del servidor: {response.StatusCode}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Fatal", "Error al procesar datos: " + ex.Message, "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void LoadNextPage()
    {
        var items = _allData.Skip(_page * PageSize).Take(PageSize).ToList(); 
        if (items.Any())
        {
            Cards.AddRange(items);
            _page++;
        }
    }

    private void cvCards_RemainingItemsThresholdReached(object sender, EventArgs e) => LoadNextPage();

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new DetailPage());
    }

    private async void cvCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Card card)
        {
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new DetailPage(card));
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var button = sender as Button;

        var card = button?.CommandParameter as Card ?? button?.BindingContext as Card;

        if (card == null) return;

        bool confirm = await DisplayAlert("Eliminar", $"¿Borrar '{card.Name}'?", "Sí", "No");
        if (!confirm) return;

        try
        {
            using var client = new HttpClient();
            if (!string.IsNullOrEmpty(LoginPage.JwtToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginPage.JwtToken);

            var url = DeviceInfo.Platform == DevicePlatform.Android
                ? $"https://10.0.2.2:53620/api/Data/{card.Id}"
                : $"https://localhost:53620/api/Data/{card.Id}";

            var response = await client.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
            {
                Cards.Remove(card);
                _allData.Remove(card);
            }
            else
            {
                await DisplayAlert("Error", "No se pudo eliminar", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}