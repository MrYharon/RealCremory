using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public class ProductStepperItem
    {
        public int ProductId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public int Quantity { get; set; }
    }

    public partial class WalkInOrderFormPage : ContentPage
    {
        private static string GetMarker(string? variant)
        {
            if (string.IsNullOrEmpty(variant)) return "[SOLO]";
            var v = variant.ToLowerInvariant();
            if (v.Contains("box of 2")) return "[BOX2]";
            if (v.Contains("box of 4")) return "[BOX4]";
            if (v.Contains("box")) return "[BOX]";
            if (v.Contains("inch") || v.Contains("round")) return "[ROUND]";
            return "[SOLO]";
        }

        private readonly ApiService _api;
        private readonly ObservableCollection<ProductStepperItem> _stepperItems = [];
        private decimal _quickTotal;

        public event EventHandler<OrderDto>? OrderCreated;

        public WalkInOrderFormPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
            ProductStepperView.ItemsSource = _stepperItems;
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
                _stepperItems.Clear();

                foreach (var group in menu)
                {
                    foreach (var item in group.Items)
                    {
                        _stepperItems.Add(new ProductStepperItem
                        {
                            ProductId = item.ProductId,
                            DisplayName = $"{item.Variant} {item.Flavor} {GetMarker(item.Variant)}".Trim(),
                            BasePrice = item.BasePrice
                        });
                    }
                }
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load menu.", "OK");
            }
        }

        private void OnStepperPlus(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.BindingContext as ProductStepperItem;
            if (item == null) return;

            item.Quantity++;
            RefreshItemsEditor();

            var idx = _stepperItems.IndexOf(item);
            if (idx >= 0)
            {
                _stepperItems.RemoveAt(idx);
                _stepperItems.Insert(idx, item);
            }
        }

        private void OnStepperMinus(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.BindingContext as ProductStepperItem;
            if (item == null || item.Quantity <= 0) return;

            item.Quantity--;
            RefreshItemsEditor();

            var idx = _stepperItems.IndexOf(item);
            if (idx >= 0)
            {
                _stepperItems.RemoveAt(idx);
                _stepperItems.Insert(idx, item);
            }
        }

        private void RefreshItemsEditor()
        {
            var selected = _stepperItems.Where(s => s.Quantity > 0).ToList();
            if (selected.Count == 0)
            {
                ItemsEditor.Text = "";
                PriceEntry.Text = "";
                return;
            }

            var lines = selected.Select(s => $"{s.Quantity}x {s.DisplayName}");
            ItemsEditor.Text = string.Join("\n", lines);

            _quickTotal = selected.Sum(s => s.BasePrice * s.Quantity);
            if (string.IsNullOrWhiteSpace(PriceEntry.Text?.Trim()))
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
            SaveButton.IsVisible = false;
            SaveLoadingOverlay.IsVisible = true;
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
                SaveButton.IsVisible = true;
                SaveLoadingOverlay.IsVisible = false;
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnCopyTemplate(object sender, TappedEventArgs e)
        {
            var template = "!ORDER SUMMARY: (name)\n* 1 x Box of 4\n  - 2 Classic Cheesecake\n  - 1 Biscoff\n  - 1 Cookies and Cream\n* 2 x Classic Cheesecake\nTotal: PHP 1670\nContact: 09170000000";
            await Clipboard.Default.SetTextAsync(template);

            var label = sender as Label;
            if (label != null)
            {
                label.Text = "Copied!";
                await Task.Delay(1500);
                label.Text = "Copy Template";
            }
        }
    }
}