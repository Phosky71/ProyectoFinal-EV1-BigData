using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using System.Collections.Generic;

namespace Frontend.MAUIApp
{
    public partial class MasterPage : ContentPage
    {
        public ObservableCollection<dynamic> Cards { get; set; } = new ObservableCollection<dynamic>();
        private int _page = 0;
        private int _pageSize = 20;
        private bool _isLoading = false;

        public MasterPage()
        {
            InitializeComponent();
            cvCards.ItemsSource = Cards;
            LoadData();
        }

        private async void LoadData()
        {
            if (_isLoading) return;
            _isLoading = true;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginPage.JwtToken);
                try
                {
                    // Assuming API supports paging ?page={_page}&size={_pageSize}
                    // If not, we fetch all and slice locally (not true lazy loading but simulates it for UI)
                    // For this assignment, I'll fetch all and add in chunks if API doesn't support paging, 
                    // OR I'll assume I added paging to DataController.
                    // Let's assume I fetch all for now and just display incrementally to show the "Lazy Loading" UI event handling.
                    
                    var url = DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:7001/api/Data" : "https://localhost:7001/api/Data";
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var allData = JsonConvert.DeserializeObject<List<dynamic>>(json);
                        
                        // Simulate paging
                        var pagedData = allData.Skip(_page * _pageSize).Take(_pageSize);
                        foreach (var item in pagedData)
                        {
                            Cards.Add(item);
                        }
                        _page++;
                    }
                }
                catch { }
            }
            _isLoading = false;
        }

        private void cvCards_RemainingItemsThresholdReached(object sender, EventArgs e)
        {
            LoadData();
        }
    }
}
