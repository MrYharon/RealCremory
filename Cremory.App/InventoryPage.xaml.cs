using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class InventoryPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly ObservableCollection<Ingredient> _allIngredients = [];
        private readonly ObservableCollection<ProductStockDto> _allStock = [];
        private List<Ingredient> _masterIngredients = [];
        private List<ProductStockDto> _masterStock = [];
        private bool _showStockTab;

        public InventoryPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
            IngredientsCollectionView.ItemsSource = _allIngredients;
            StockCollectionView.ItemsSource = _allStock;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _pulseLowStock = false;
        }

        private bool _pulseLowStock;
        private async void StartLowStockPulse()
        {
            if (_pulseLowStock) return;
            var hasLowStock = _masterStock.Any(s => s.IsLowStock);
            if (!hasLowStock) return;

            _pulseLowStock = true;
            while (_pulseLowStock)
            {
                await StockCollectionView.FadeTo(0.85, 800, Easing.SinInOut);
                await StockCollectionView.FadeTo(1.0, 800, Easing.SinInOut);
            }
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
                if (_showStockTab)
                    await LoadStockAsync();
                else
                    await LoadIngredientsAsync();
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async Task LoadIngredientsAsync()
        {
            try
            {
                var ingredients = await _api.GetIngredientsAsync();
                if (ingredients.Count == 0)
                    ingredients = GetSampleIngredients();
                _masterIngredients = ingredients;
                ApplySearchFilter();
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load inventory. Check connection.", "OK");
            }
        }

        private async Task LoadStockAsync()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SkeletonLoading.IsVisible = true;
                StockCollectionView.IsVisible = false;
                _ = AnimateSkeleton();
            });
            try
            {
                var stock = await _api.GetProductStockAsync();
                _masterStock = stock;
                ApplySearchFilter();
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load stock. Check connection.", "OK");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SkeletonLoading.IsVisible = false;
                    StockCollectionView.IsVisible = true;
                    StartLowStockPulse();
                });
            }
        }

        private bool _skeletonAnimating;
        private async Task AnimateSkeleton()
        {
            if (_skeletonAnimating) return;
            _skeletonAnimating = true;
            while (SkeletonLoading.IsVisible)
            {
                await SkeletonLoading.FadeTo(0.4, 600, Easing.SinInOut);
                await SkeletonLoading.FadeTo(1.0, 600, Easing.SinInOut);
            }
            _skeletonAnimating = false;
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
            var searchText = InventorySearchBar?.Text?.Trim().ToLower() ?? "";
            if (_showStockTab)
            {
                var filtered = string.IsNullOrEmpty(searchText)
                    ? _masterStock
                    : _masterStock.Where(s =>
                        (s.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.Variant?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.Flavor?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
                _allStock.Clear();
                foreach (var item in filtered) _allStock.Add(item);
            }
            else
            {
                var filtered = string.IsNullOrEmpty(searchText)
                    ? _masterIngredients
                    : _masterIngredients.Where(i => i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
                _allIngredients.Clear();
                foreach (var item in filtered) _allIngredients.Add(item);
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void OnIngredientsTabClicked(object sender, EventArgs e)
        {
            _showStockTab = false;
            IngredientsTabBtn.BackgroundColor = (Color)Resources["Primary"];
            IngredientsTabBtn.TextColor = Colors.White;
            FinishedStockTabBtn.BackgroundColor = (Color)Resources["Gray200"];
            FinishedStockTabBtn.TextColor = (Color)Resources["Gray700"];
            IngredientsCollectionView.IsVisible = true;
            StockCollectionView.IsVisible = false;
            _ = LoadDataAsync();
        }

        private void OnFinishedStockTabClicked(object sender, EventArgs e)
        {
            _showStockTab = true;
            FinishedStockTabBtn.BackgroundColor = (Color)Resources["Primary"];
            FinishedStockTabBtn.TextColor = Colors.White;
            IngredientsTabBtn.BackgroundColor = (Color)Resources["Gray200"];
            IngredientsTabBtn.TextColor = (Color)Resources["Gray700"];
            IngredientsCollectionView.IsVisible = false;
            StockCollectionView.IsVisible = true;
            _ = LoadDataAsync();
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
                        await LoadIngredientsAsync();
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
                    await LoadIngredientsAsync();
                else
                {
                    _masterIngredients.RemoveAll(i => i.IngredientId == ingredient.IngredientId);
                    ApplySearchFilter();
                }
            }
        }

        private async void OnSetExactStockClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var stockItem = btn?.BindingContext as ProductStockDto;
            if (stockItem == null) return;

            var qtyStr = await DisplayPromptAsync("Set Stock",
                $"Current stock: {stockItem.CurrentStock}\n{stockItem.Variant} — {stockItem.Flavor}\nEnter exact quantity:",
                "Save", "Cancel",
                placeholder: stockItem.CurrentStock.ToString(),
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyStr)) return;
            if (!int.TryParse(qtyStr, out var newQty) || newQty < 0) return;

            var success = await _api.UpdateProductStockAsync(stockItem.ProductId, newQty);
            if (success)
            {
                stockItem.CurrentStock = newQty;
                var idx = _masterStock.FindIndex(s => s.ProductId == stockItem.ProductId);
                if (idx >= 0) _masterStock[idx] = stockItem;
                ApplySearchFilter();
                await DisplayAlert("Updated", $"{stockItem.Name} stock set to {newQty}.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to update stock.", "OK");
            }
        }

        private async void OnRecordProductionClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var stockItem = btn?.BindingContext as ProductStockDto;
            if (stockItem == null) return;

            var qtyStr = await DisplayPromptAsync("Record Production",
                $"Current stock: {stockItem.CurrentStock}\n{stockItem.Variant} — {stockItem.Flavor}\nHow many pieces were produced?",
                "Add to Stock", "Cancel",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyStr)) return;
            if (!int.TryParse(qtyStr, out var produced) || produced <= 0) return;

            var newQty = stockItem.CurrentStock + produced;
            var success = await _api.UpdateProductStockAsync(stockItem.ProductId, newQty);
            if (success)
            {
                stockItem.CurrentStock = newQty;
                var idx = _masterStock.FindIndex(s => s.ProductId == stockItem.ProductId);
                if (idx >= 0) _masterStock[idx] = stockItem;
                ApplySearchFilter();
                await DisplayAlert("Stock Updated",
                    $"Added {produced} units of {stockItem.Name}.\nNew stock: {newQty}", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to update stock.", "OK");
            }
        }

        private async void OnIncrementStockClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var stockItem = btn?.BindingContext as ProductStockDto;
            if (stockItem == null) return;

            var newQty = stockItem.CurrentStock + 1;
            var success = await _api.UpdateProductStockAsync(stockItem.ProductId, newQty);
            if (success)
            {
                stockItem.CurrentStock = newQty;
                var idx = _masterStock.FindIndex(s => s.ProductId == stockItem.ProductId);
                if (idx >= 0) _masterStock[idx] = stockItem;
                ApplySearchFilter();
            }
        }

        private async void OnDecrementStockClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var stockItem = btn?.BindingContext as ProductStockDto;
            if (stockItem == null) return;

            if (stockItem.CurrentStock <= 0) return;

            var newQty = stockItem.CurrentStock - 1;
            var success = await _api.UpdateProductStockAsync(stockItem.ProductId, newQty);
            if (success)
            {
                stockItem.CurrentStock = newQty;
                var idx = _masterStock.FindIndex(s => s.ProductId == stockItem.ProductId);
                if (idx >= 0) _masterStock[idx] = stockItem;
                ApplySearchFilter();
            }
        }
    }
}