using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class WalkInOrderFormPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly List<Product> _products = [];
        private decimal _quickTotal;

        public event EventHandler<OrderDto>? OrderCreated;

        public WalkInOrderFormPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadProductsAsync();
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var menu = await _api.GetMenuAsync();
                _products.Clear();
                ProductChips.Children.Clear();

                foreach (var group in menu)
                {
                    foreach (var item in group.Items)
                    {
                        _products.Add(new Product
                        {
                            ProductId = item.ProductId,
                            Name = $"{item.Variant} {item.Flavor}".Trim(),
                            BasePrice = item.BasePrice
                        });

                        var chip = new Border
                        {
                            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                            BackgroundColor = (Color)Application.Current!.Resources["Gray100"],
                            Stroke = (Color)Application.Current!.Resources["Gray300"],
                            StrokeThickness = 1,
                            Padding = new Thickness(12, 6),
                            Margin = new Thickness(0, 0, 6, 6),
                            HeightRequest = 34
                        };

                        var label = new Label
                        {
                            Text = $"{item.Variant} {item.Flavor}".Trim(),
                            FontSize = 12,
                            TextColor = (Color)Application.Current!.Resources["Gray800"]
                        };

                        var tap = new TapGestureRecognizer();
                        var capturedProduct = _products[^1];
                        tap.Tapped += (s, e) => OnProductTapped(capturedProduct);
                        label.GestureRecognizers.Add(tap);

                        chip.Content = label;
                        ProductChips.Children.Add(chip);
                    }
                }

                if (_products.Count == 0)
                {
                    ProductChips.Children.Add(new Label
                    {
                        Text = "No products available",
                        FontSize = 12,
                        TextColor = (Color)Application.Current!.Resources["Gray400"]
                    });
                }
            }
            catch
            {
                ProductChips.Children.Add(new Label
                {
                    Text = "Could not load products",
                    FontSize = 12,
                    TextColor = (Color)Application.Current!.Resources["Gray500"]
                });
            }
        }

        private void OnProductTapped(Product product)
        {
            var currentItems = ItemsEditor?.Text?.Trim();
            var newItem = product.Name;
            if (!string.IsNullOrWhiteSpace(currentItems))
                newItem = $"{currentItems}\n• {product.Name}";
            else
                newItem = $"• {product.Name}";

            if (ItemsEditor != null)
                ItemsEditor.Text = newItem;

            _quickTotal += product.BasePrice;
            if (PriceEntry != null && string.IsNullOrWhiteSpace(PriceEntry.Text?.Trim()))
            {
                PriceEntry.Text = _quickTotal.ToString("F2");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var name = NameEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Validation", "Customer name is required.", "OK");
                return;
            }

            var items = ItemsEditor?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(items))
            {
                await DisplayAlert("Validation", "Items are required.", "OK");
                return;
            }

            if (!decimal.TryParse(PriceEntry?.Text?.Trim(), out var price) || price <= 0)
            {
                await DisplayAlert("Validation", "Enter a valid total price.", "OK");
                return;
            }

            var contact = ContactEntry?.Text?.Trim();

            SaveButton.IsEnabled = false;
            SaveButton.Text = "Saving...";
            StatusLabel.Text = "";

            try
            {
                var result = await _api.CreateWalkInOrderAsync(name, items, price, contact);
                if (result != null)
                {
                    StatusLabel.TextColor = Colors.Green;
                    StatusLabel.Text = $"Order created: {result.OrderId}";
                    OrderCreated?.Invoke(this, result);
                    await Navigation.PopModalAsync();
                }
                else
                {
                    StatusLabel.TextColor = Colors.Red;
                    StatusLabel.Text = "Failed to create order. Check API.";
                }
            }
            catch
            {
                StatusLabel.TextColor = Colors.Red;
                StatusLabel.Text = "Connection error.";
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Text = "Create Order";
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
