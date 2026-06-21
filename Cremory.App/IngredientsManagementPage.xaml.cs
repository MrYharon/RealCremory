using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class IngredientsManagementPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly ObservableCollection<Ingredient> _allIngredients = [];
        private List<Ingredient> _masterList = [];

        public IngredientsManagementPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadIngredientsAsync();
        }

        private async Task LoadIngredientsAsync()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var ingredients = await _api.GetIngredientsAsync();
                if (ingredients.Count == 0)
                    ingredients = GetSampleIngredients();

                _masterList = ingredients;
                ApplySearchFilter();
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load ingredients. Check connection.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private static List<Ingredient> GetSampleIngredients()
        {
            return
            [
                new() { IngredientId = 1, Name = "All-Purpose Flour", StockQuantity = 25, Unit = "kg", ReorderLevel = 10 },
                new() { IngredientId = 2, Name = "White Sugar", StockQuantity = 15, Unit = "kg", ReorderLevel = 8 },
                new() { IngredientId = 3, Name = "Butter (Unsalted)", StockQuantity = 8, Unit = "kg", ReorderLevel = 5 },
                new() { IngredientId = 4, Name = "Fresh Eggs", StockQuantity = 120, Unit = "pcs", ReorderLevel = 60 },
                new() { IngredientId = 5, Name = "Whole Milk", StockQuantity = 4, Unit = "L", ReorderLevel = 6 },
                new() { IngredientId = 6, Name = "Cream Cheese", StockQuantity = 3, Unit = "kg", ReorderLevel = 4 },
                new() { IngredientId = 7, Name = "Active Dry Yeast", StockQuantity = 500, Unit = "g", ReorderLevel = 200 },
                new() { IngredientId = 8, Name = "Vanilla Extract", StockQuantity = 1, Unit = "L", ReorderLevel = 0.5m },
                new() { IngredientId = 9, Name = "Baking Powder", StockQuantity = 2, Unit = "kg", ReorderLevel = 1 },
                new() { IngredientId = 10, Name = "Salt", StockQuantity = 5, Unit = "kg", ReorderLevel = 2 },
                new() { IngredientId = 11, Name = "Cocoa Powder", StockQuantity = 2.5m, Unit = "kg", ReorderLevel = 1.5m },
                new() { IngredientId = 12, Name = "Ube Halaya", StockQuantity = 1, Unit = "kg", ReorderLevel = 2 }
            ];
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

            IngredientsCollectionView.ItemsSource = _allIngredients;
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
                        result.IngredientId = _masterList.Count > 0 ? _masterList.Max(i => i.IngredientId) + 1 : 1;
                        _masterList.Add(result);
                        ApplySearchFilter();
                    }
                }
                finally
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                }
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
                    await DisplayAlert("Deleted", $"Ingredient \"{ingredient.Name}\" deleted.", "OK");
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
