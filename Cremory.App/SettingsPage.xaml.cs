using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class SettingsPage : ContentPage
    {
        private readonly ApiService _api;

        public SettingsPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSettingsAsync();
            await LoadLowStockAsync();
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var enabled = await _api.GetAutoDeductEnabledAsync();
                AutoDeductSwitch.IsToggled = enabled;
                AutoDeductStatus.Text = enabled ? "Stock deduction is ON" : "Stock deduction is OFF";
            }
            catch
            {
                AutoDeductStatus.Text = "Failed to load setting";
            }
        }

        private async Task LoadLowStockAsync()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            LowStockList.Children.Clear();
            try
            {
                var lowStock = await _api.GetLowStockProductsAsync();
                if (lowStock.Count == 0)
                {
                    LowStockList.Children.Add(new Label
                    {
                        Text = "All products are well-stocked",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#6B7280"),
                        HorizontalTextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0)
                    });
                }
                else
                {
                    foreach (var p in lowStock)
                        LowStockList.Children.Add(BuildLowStockCard(p));
                }
            }
            catch
            {
                LowStockList.Children.Add(new Label
                {
                    Text = "Failed to load low stock products",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#DC2626"),
                    HorizontalTextAlignment = TextAlignment.Center
                });
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private static Border BuildLowStockCard(LowStockProductDto p)
        {
            var nameLabel = new Label
            {
                Text = $"{p.Name} - {p.Variant} ({p.Flavor})",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#991B1B")
            };

            var stockLabel = new Label
            {
                Text = $"Stock: {p.CurrentStock} / {p.LowStockThreshold} {p.Unit}",
                FontSize = 12,
                TextColor = Color.FromArgb("#B91C1C")
            };

            var badge = new Label
            {
                Text = "Low!",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#DC2626"),
                Padding = new Thickness(8, 3),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                ],
                RowDefinitions =
                [
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto)
                ],
                RowSpacing = 4
            };

            Grid.SetRow(nameLabel, 0);
            Grid.SetColumn(nameLabel, 0);
            grid.Children.Add(nameLabel);

            Grid.SetRow(stockLabel, 1);
            Grid.SetColumn(stockLabel, 0);
            grid.Children.Add(stockLabel);

            Grid.SetRow(badge, 0);
            Grid.SetColumn(badge, 1);
            Grid.SetRowSpan(badge, 2);
            grid.Children.Add(badge);

            return new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                BackgroundColor = Colors.White,
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 8),
                Stroke = Color.FromArgb("#FECACA"),
                Content = grid
            };
        }

        private async void OnAutoDeductToggled(object sender, ToggledEventArgs e)
        {
            var success = await _api.SetAutoDeductEnabledAsync(e.Value);
            AutoDeductStatus.Text = success
                ? (e.Value ? "Stock deduction is ON" : "Stock deduction is OFF")
                : "Failed to save setting";
        }
    }
}
