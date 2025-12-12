using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace MauiApp.UI;

public partial class DetailPage : ContentPage
{
    private readonly Card _existingCard;

    public DetailPage()
    {
        InitializeComponent();
    }

    public DetailPage(Card card) : this()
    {
        _existingCard = card;
        txtName.Text = card.Name;
        txtMana.Text = card.ManaCost;
        txtType.Text = card.Type;
        txtRarity.Text = card.Rarity;
        txtSet.Text = card.SetName;
        txtText.Text = card.Text;
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            await DisplayAlert("Error", "El nombre es obligatorio", "OK");
            return;
        }

        var cardDto = new Card
        {
            Id = _existingCard?.Id ?? Guid.NewGuid(),
            Name = txtName.Text,
            ManaCost = txtMana.Text,
            Type = txtType.Text,
            Rarity = txtRarity.Text,
            SetName = txtSet.Text,
            Text = txtText.Text
        };

        using var client = new HttpClient();
        if (!string.IsNullOrEmpty(LoginPage.JwtToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginPage.JwtToken);

        var content = new StringContent(JsonConvert.SerializeObject(cardDto), Encoding.UTF8, "application/json");

        string url;
        HttpResponseMessage response;

        try
        {
            if (_existingCard == null)
            {
                url = DeviceInfo.Platform == DevicePlatform.Android
                    ? "https://10.0.2.2:53620/api/Data"
                    : "https://localhost:53620/api/Data";

                response = await client.PostAsync(url, content);
            }
            else
            {
                var id = _existingCard.Id;
                url = DeviceInfo.Platform == DevicePlatform.Android
                    ? $"https://10.0.2.2:53620/api/Data/{id}"
                    : $"https://localhost:53620/api/Data/{id}";

                response = await client.PutAsync(url, content);
            }

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "Guardado correctamente", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"Fallo al guardar: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}