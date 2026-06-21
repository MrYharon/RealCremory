using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public class CategoryGroup : List<MenuItemDto>
    {
        public string Name { get; }
        public CategoryGroup(string name, List<MenuItemDto> items) : base(items)
        {
            Name = name;
        }
    }

    public partial class MenuPage : ContentPage
    {
        private readonly ApiService _api;

        public MenuPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadMenu();
        }

        private async Task LoadMenu()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var apiCategories = await _api.GetMenuAsync();
                if (apiCategories == null) return;
            var grouped = apiCategories
                .Select(c => new CategoryGroup(c.CategoryName, c.Items))
                .ToList();
                MenuCollectionView.ItemsSource = grouped;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load menu. Check connection.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }
    }
}
