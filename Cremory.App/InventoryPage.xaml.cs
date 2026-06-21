using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class InventoryPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly ObservableCollection<Ingredient> _allIngredients = [];
        private List<Ingredient> _masterList = [];

        public InventoryPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
            IngredientsCollectionView.ItemsSource = _allIngredients;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadIngredientsAsync();
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadIngredientsAsync();
            InventoryRefreshView.IsRefreshing = false;
        }

        private async Task LoadIngredientsAsync()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var ingredients = await _api.GetIngredientsAsync();
                _masterList = ingredients;
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
            var searchText = IngredientSearchBar?.Text?.Trim().ToLower() ?? "";
            var filtered = string.IsNullOrEmpty(searchText)
                ? _masterList
                : _masterList.Where(i => i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

            _allIngredients.Clear();
            foreach (var ingredient in filtered)
                _allIngredients.Add(ingredient);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private async void OnAddClicked(object sender, EventArgs e)
        {
            var formPage = new IngredientFormPage();
            await Navigation.PushModalAsync(new NavigationPage(formPage));
            var result = await formPage.Result;

            if (result != null)
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                try
                {
                    var (created, error) = await _api.CreateIngredientAsync(result);
                    if (created)
                    {
                        await DisplayAlert("Success", $"Ingredient \"{result.Name}\" created.", "OK");
                        await LoadIngredientsAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", error ?? "Failed to create ingredient.", "OK");
                    }
                }
                finally
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                }
            }
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
                    var idx = _masterList.FindIndex(i => i.IngredientId == ingredient.IngredientId);
                    if (idx >= 0) _masterList[idx] = ingredient;
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
                        await LoadIngredientsAsync();
                    }
                    else
                    {
                        var index = _masterList.FindIndex(i => i.IngredientId == ingredient.IngredientId);
                        if (index >= 0) _masterList[index] = result;
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
                {
                    await LoadIngredientsAsync();
                }
                else
                {
                    _masterList.RemoveAll(i => i.IngredientId == ingredient.IngredientId);
                    ApplySearchFilter();
                }
            }
        }
    }
}
