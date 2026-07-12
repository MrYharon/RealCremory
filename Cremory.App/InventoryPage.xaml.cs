using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class InventoryPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly ObservableCollection<Ingredient> _allIngredients = [];
        private List<Ingredient> _masterIngredients = [];

        public InventoryPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
            IngredientsCollectionView.ItemsSource = _allIngredients;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadDataAsync();
            InventoryRefreshView.IsRefreshing = false;
        }

        private async Task LoadDataAsync()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var ingredients = await _api.GetIngredientsAsync();
                _masterIngredients = ingredients;
                ApplySearchFilter();
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load inventory. Check connection.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private void ApplySearchFilter()
        {
            var searchText = InventorySearchBar?.Text?.Trim().ToLower() ?? "";
            var filtered = string.IsNullOrEmpty(searchText)
                ? _masterIngredients
                : _masterIngredients.Where(i => i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            _allIngredients.Clear();
            foreach (var item in filtered) _allIngredients.Add(item);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private async void OnAdjustStockSwipe(object sender, EventArgs e)
        {
            var item = sender as SwipeItem;
            var ingredient = item?.BindingContext as Ingredient;
            if (ingredient == null) return;

            var qtyStr = await DisplayPromptAsync("Adjust Stock",
                $"Current stock: {ingredient.StockQuantity} {ingredient.Unit}\nEnter new quantity:",
                "Save", "Cancel",
                placeholder: ingredient.StockQuantity.ToString(),
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyStr)) return;
            if (!decimal.TryParse(qtyStr, out var newQty)) return;

            ingredient.StockQuantity = newQty;

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var (updated, error) = await _api.UpdateIngredientAsync(ingredient);
                if (updated)
                {
                    var idx = _masterIngredients.FindIndex(i => i.IngredientId == ingredient.IngredientId);
                    if (idx >= 0) _masterIngredients[idx] = ingredient;
                    ApplySearchFilter();
                    await DisplayAlert("Updated",
                        $"{ingredient.Name} stock set to {ingredient.StockQuantity} {ingredient.Unit}.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", error ?? "Failed to update stock.", "OK");
                }
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnEditSwipe(object sender, EventArgs e)
        {
            var item = sender as SwipeItem;
            var ingredient = item?.BindingContext as Ingredient;
            if (ingredient == null) return;

            var formPage = new IngredientFormPage(ingredient);
            await Navigation.PushModalAsync(new NavigationPage(formPage));
            var result = await formPage.Result;

            if (result != null)
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                try
                {
                    result.IngredientId = ingredient.IngredientId;
                    var (updated, updateErr) = await _api.UpdateIngredientAsync(result);
                    if (updated)
                    {
                        await DisplayAlert("Success", "Ingredient updated.", "OK");
                        await LoadDataAsync();
                    }
                    else
                    {
                        var index = _masterIngredients.FindIndex(i => i.IngredientId == ingredient.IngredientId);
                        if (index >= 0) _masterIngredients[index] = result;
                        ApplySearchFilter();
                        await DisplayAlert("Error", updateErr ?? "Failed to update ingredient.", "OK");
                    }
                }
                finally
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                }
            }
        }

        private async void OnDeleteSwipe(object sender, EventArgs e)
        {
            var item = sender as SwipeItem;
            var ingredient = item?.BindingContext as Ingredient;
            if (ingredient == null) return;

            var confirm = await DisplayAlert("Delete Ingredient",
                $"Are you sure you want to delete {ingredient.Name}?", "Delete", "Cancel");

            if (confirm)
            {
                var deleted = await _api.DeleteIngredientAsync(ingredient.IngredientId);
                if (deleted)
                    await LoadDataAsync();
                else
                {
                    _masterIngredients.RemoveAll(i => i.IngredientId == ingredient.IngredientId);
                    ApplySearchFilter();
                }
            }
        }
    }
}
