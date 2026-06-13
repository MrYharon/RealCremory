using Cremory.App.Services;

namespace Cremory.App
{
    public partial class InventoryPage : ContentPage
    {
        private readonly ApiService _apiService;

        public InventoryPage()
        {
            InitializeComponent();
            _apiService = new ApiService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadInventoryData();
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadInventoryData();
        }

        private async Task LoadInventoryData()
        {
            var ingredients = await _apiService.GetIngredientsAsync();
            IngredientsCollectionView.ItemsSource = ingredients;
        }
    }
}