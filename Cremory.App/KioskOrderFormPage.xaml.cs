using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class KioskOrderFormPage : ContentPage
    {
        private readonly ApiService _api;
        private List<MenuCategoryDto> _menu = [];
        private MenuCategoryDto? _activeCategory;
        private readonly ObservableCollection<CartItem> _cart = [];
        private readonly Dictionary<int, Button> _addButtons = [];

        public event EventHandler<OrderDto>? OrderCreated;

        public KioskOrderFormPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
            CartCollection.ItemsSource = _cart;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadMenuAsync();
        }

        private async Task LoadMenuAsync()
        {
            try
            {
                _menu = await _api.GetMenuAsync();
                BuildCategoryChips();
                if (_menu.Count > 0)
                    SelectCategory(_menu[0]);
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load menu.", "OK");
            }
        }

        private void BuildCategoryChips()
        {
            CategoryChips.Children.Clear();
            foreach (var cat in _menu)
            {
                var border = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                    BackgroundColor = Colors.Transparent,
                    Stroke = Color.FromArgb("#E8D4D4"),
                    StrokeThickness = 1,
                    Padding = new Thickness(14, 4),
                    HeightRequest = 30
                };
                var label = new Label
                {
                    Text = cat.CategoryName,
                    FontSize = 11,
                    TextColor = Color.FromArgb("#B89292"),
                    VerticalOptions = LayoutOptions.Center
                };
                border.Content = label;

                var tapCat = cat;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (s, e) => SelectCategory(tapCat);
                border.GestureRecognizers.Add(tap);

                CategoryChips.Children.Add(border);
            }
        }

        private void SelectCategory(MenuCategoryDto cat)
        {
            _activeCategory = cat;

            foreach (var child in CategoryChips.Children)
            {
                if (child is Border b && b.Content is Label l)
                {
                    var isActive = l.Text == cat.CategoryName;
                    b.BackgroundColor = isActive ? Color.FromArgb("#E27575") : Colors.Transparent;
                    b.Stroke = isActive ? Colors.Transparent : Color.FromArgb("#E8D4D4");
                    l.TextColor = isActive ? Colors.White : Color.FromArgb("#B89292");
                    l.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
                }
            }

            ProductContainer.Children.Clear();
            _addButtons.Clear();

            foreach (var item in cat.Items)
            {
                var card = BuildProductCard(item);
                ProductContainer.Children.Add(card);
            }
        }

        private View BuildProductCard(MenuItemDto item)
        {
            var border = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#F0E0E0"),
                StrokeThickness = 1,
                Padding = new Thickness(14, 10),
                Margin = new Thickness(0, 0, 0, 0)
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10
            };

            var infoStack = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };

            if (!string.IsNullOrWhiteSpace(item.Flavor))
            {
                infoStack.Children.Add(new Label
                {
                    Text = item.Flavor,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#2C2525")
                });
            }

            var nameParts = new List<string> { item.Name };
            if (!string.IsNullOrWhiteSpace(item.Variant))
                nameParts.Add(item.Variant);
            infoStack.Children.Add(new Label
            {
                Text = string.Join(" · ", nameParts),
                FontSize = 11,
                TextColor = Color.FromArgb("#B89292")
            });

            infoStack.Children.Add(new Label
            {
                Text = $"₱{item.BasePrice:N2}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#E27575"),
                Margin = new Thickness(0, 4, 0, 0)
            });

            if (!string.IsNullOrWhiteSpace(item.AddOnDescription))
            {
                infoStack.Children.Add(new Label
                {
                    Text = item.AddOnDescription,
                    FontSize = 9,
                    TextColor = Color.FromArgb("#B89292")
                });
            }

            grid.Add(infoStack, 0);

            var addBtn = new Button
            {
                Text = "+",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#E27575"),
                WidthRequest = 40,
                HeightRequest = 40,
                CornerRadius = 20,
                Padding = new Thickness(0),
                VerticalOptions = LayoutOptions.Center
            };

            var captured = item;
            addBtn.Clicked += (s, e) => AddToCart(captured);
            grid.Add(addBtn, 1);

            border.Content = grid;
            return border;
        }

        private void AddToCart(MenuItemDto item)
        {
            var existing = _cart.FirstOrDefault(c =>
                c.ProductId == item.ProductId);
            if (existing != null)
            {
                existing.Qty++;
            }
            else
            {
                _cart.Add(new CartItem
                {
                    ProductId = item.ProductId,
                    Name = item.Name,
                    Variant = item.Variant,
                    Flavor = item.Flavor,
                    BasePrice = item.BasePrice,
                    Qty = 1
                });
            }
            UpdateSummary();
        }

        private void OnCartDecrease(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is CartItem item)
            {
                if (item.Qty <= 1)
                    _cart.Remove(item);
                else
                    item.Qty--;
                UpdateSummary();
            }
        }

        private void OnCartIncrease(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is CartItem item)
            {
                item.Qty++;
                UpdateSummary();
            }
        }

        private void UpdateSummary()
        {
            var total = _cart.Sum(c => c.Subtotal);
            TotalLabel.Text = $"₱{total:N2}";
            var hasItems = _cart.Count > 0;
            SubmitBtn.IsEnabled = hasItems;
            SubmitBtn.Opacity = hasItems ? 1.0 : 0.5;
        }

        private void OnDeliveryTypeChanged(object? sender, EventArgs e)
        {
            var isDelivery = DeliveryTypePicker.SelectedItem?.ToString() == "Delivery";
            AddressBorder.IsVisible = isDelivery;
        }

        private async void OnSubmitClicked(object? sender, EventArgs e)
        {
            var name = NameEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Validation", "Customer name is required.", "OK");
                return;
            }

            if (_cart.Count == 0)
            {
                await DisplayAlert("Validation", "Add at least one item.", "OK");
                return;
            }

            var itemsText = string.Join("\n", _cart.Select(c =>
                $"* {c.Qty} x {c.Name}" +
                (string.IsNullOrWhiteSpace(c.Variant) ? "" : $" ({c.Variant})") +
                (string.IsNullOrWhiteSpace(c.Flavor) ? "" : $" - {c.Flavor}")
            ));

            var totalPrice = _cart.Sum(c => c.Subtotal);
            var contact = ContactEntry?.Text?.Trim();
            var deliveryType = DeliveryTypePicker.SelectedItem?.ToString();
            var address = AddressEntry?.Text?.Trim();
            var paymentStatus = PaymentPicker.SelectedItem?.ToString();

            SubmitBtn.IsEnabled = false;
            SubmitBtn.Text = "Saving...";

            try
            {
                var result = await _api.CreateWalkInOrderAsync(name, itemsText, totalPrice, contact,
                    deliveryType, address, paymentStatus);
                if (result != null)
                {
                    OrderCreated?.Invoke(this, result);
                    await Navigation.PopModalAsync();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to create order.", "OK");
                }
            }
            catch
            {
                await DisplayAlert("Error", "Connection error.", "OK");
            }
            finally
            {
                SubmitBtn.IsEnabled = true;
                SubmitBtn.Text = "Submit";
            }
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnMenuRefreshing(object? sender, EventArgs e)
        {
            await LoadMenuAsync();
            MenuRefreshView.IsRefreshing = false;
        }
    }
}
