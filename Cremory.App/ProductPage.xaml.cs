using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class ProductPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly ObservableCollection<Product> _products = [];
        private List<Product> _allProducts = [];

        public ProductPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
            ProductsCollectionView.ItemsSource = _products;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadProductsAsync();
        }

        private async Task LoadProductsAsync()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                _allProducts = await _api.GetProductsAsync();
                _products.Clear();
                foreach (var p in _allProducts)
                    _products.Add(p);
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load products. Check connection.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadProductsAsync();
            ProductRefreshView.IsRefreshing = false;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var query = e.NewTextValue?.Trim() ?? "";
            _products.Clear();
            foreach (var p in _allProducts.Where(p =>
                string.IsNullOrWhiteSpace(query) ||
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.Variant?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Flavor?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)))
                _products.Add(p);
        }

        private async void OnToggleActiveSwipe(object sender, EventArgs e)
        {
            var item = sender as SwipeItem;
            var product = item?.BindingContext as Product;
            if (product == null) return;

            product.IsActive = !product.IsActive;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var (updated, error) = await _api.UpdateProductAsync(product);
                if (updated)
                {
                    var idx = _products.IndexOf(product);
                    if (idx >= 0)
                    {
                        _products.RemoveAt(idx);
                        _products.Insert(idx, product);
                    }
                }
                else
                {
                    product.IsActive = !product.IsActive;
                    await DisplayAlert("Error", error ?? "Failed to toggle product.", "OK");
                }
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnAddClicked(object sender, EventArgs e)
        {
            var categories = await _api.GetCategoriesAsync();
            if (categories.Count == 0)
            {
                await DisplayAlert("Error", "No categories found. Add categories in the database first.", "OK");
                return;
            }

            var formPage = new ProductFormPage(categories);
            await Navigation.PushModalAsync(new NavigationPage(formPage));
            var result = await formPage.Result;

            if (result != null)
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                try
                {
                    var (created, error) = await _api.CreateProductAsync(result);
                    if (created)
                    {
                        await DisplayAlert("Success", $"Product \"{result.Name}\" created.", "OK");
                        await LoadProductsAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", error ?? "Failed to create product.", "OK");
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
            var product = item?.BindingContext as Product;
            if (product == null) return;

            var categories = await _api.GetCategoriesAsync();
            if (categories.Count == 0)
            {
                await DisplayAlert("Error", "No categories found.", "OK");
                return;
            }

            var formPage = new ProductFormPage(categories, product);
            await Navigation.PushModalAsync(new NavigationPage(formPage));
            var result = await formPage.Result;

            if (result != null)
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                try
                {
                    result.ProductId = product.ProductId;
                    var (updated, updateErr) = await _api.UpdateProductAsync(result);
                    if (updated)
                    {
                        await DisplayAlert("Success", "Product updated.", "OK");
                        await LoadProductsAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", updateErr ?? "Failed to update product.", "OK");
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
            var product = item?.BindingContext as Product;
            if (product == null) return;

            var confirm = await DisplayAlert("Delete Product",
                $"Delete {product.Name}?", "Delete", "Cancel");
            if (!confirm) return;

            var deleted = await _api.DeleteProductAsync(product.ProductId);
            if (deleted)
            {
                await LoadProductsAsync();
                await DisplayAlert("Deleted", $"Product \"{product.Name}\" deleted.", "OK");
            }
            else
                await DisplayAlert("Error", "Failed to delete product.", "OK");
        }
    }
}
