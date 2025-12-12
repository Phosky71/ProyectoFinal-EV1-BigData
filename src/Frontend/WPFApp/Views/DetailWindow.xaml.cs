using System;
using System.Linq;
using System.Windows;
using Frontend.WPFApp.Models;
using Frontend.WPFApp.Services;
using Frontend.WPFApp.Validators;

namespace Frontend.WPFApp
{
    public partial class DetailWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly string _cardId;

        public DetailWindow(ApiService apiService, string cardId)
        {
            InitializeComponent();
            _apiService = apiService;
            _cardId = cardId;

            Loaded += async (s, e) =>
            {
                if (!string.IsNullOrEmpty(_cardId))
                {
                    txtTitle.Text = "Editar Carta";
                    await LoadCardDataAsync();
                }
                else
                {
                    txtTitle.Text = "Nueva Carta";
                }
            };
        }

        private async System.Threading.Tasks.Task LoadCardDataAsync()
        {
            try
            {
                var card = await _apiService.GetCardByIdAsync(_cardId);

                txtName.Text = card.Name;
                txtManaCost.Text = card.ManaCost;
                txtType.Text = card.Type;
                txtRarity.Text = card.Rarity;
                txtSet.Text = card.Set;
                txtSetName.Text = card.SetName;
                txtText.Text = card.Text;
                txtPower.Text = card.Power;
                txtToughness.Text = card.Toughness;
                txtImageUrl.Text = card.ImageUrl;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando carta: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var card = new Card
            {
                Id = _cardId ?? string.Empty,
                Name = txtName.Text,
                ManaCost = txtManaCost.Text,
                Type = txtType.Text,
                Rarity = txtRarity.Text,
                Set = txtSet.Text,
                SetName = txtSetName.Text,
                Text = txtText.Text,
                Power = txtPower.Text,
                Toughness = txtToughness.Text,
                ImageUrl = txtImageUrl.Text
            };

            var errors = CardValidator.Validate(card);

            if (errors.Any())
            {
                txtErrors.Text = string.Join("\n", errors);
                return;
            }

            try
            {
                bool success;

                if (!string.IsNullOrEmpty(_cardId))
                    success = await _apiService.UpdateCardAsync(card);
                else
                    success = await _apiService.CreateCardAsync(card);

                if (success)
                {
                    MessageBox.Show("Carta guardada correctamente", "Exito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
                else
                {
                    txtErrors.Text = "Error al guardar la carta";
                }
            }
            catch (Exception ex)
            {
                txtErrors.Text = $"Error: {ex.Message}";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
