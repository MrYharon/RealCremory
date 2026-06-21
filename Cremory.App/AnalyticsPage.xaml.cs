using Cremory.App.Services;

namespace Cremory.App
{
    public partial class AnalyticsPage : ContentPage
    {
        private readonly ApiService _api;

        public AnalyticsPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAnalytics();
        }

        private async Task LoadAnalytics()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var data = await _api.GetDashboardAnalyticsAsync();
                if (data == null) return;

                PopularItemsCollection.ItemsSource = data.PopularItems;

                FbPctLabel.Text = $"{data.FacebookPct}%";
                WalkInPctLabel.Text = $"{data.WalkInPct}%";
                FbProgress.Progress = data.FacebookPct / 100.0;
                WalkInProgress.Progress = data.WalkInPct / 100.0;

                LowStockLabel.Text = data.LowStockCount.ToString();
                AvgOrderValueLabel.Text = $"₱{data.AvgOrderValue:N0}";

                BuildWeeklyChart(data.WeeklySales, data.WeekLabels);
            }
            catch (Exception ex)
            {
                FbPctLabel.Text = "0%";
                WalkInPctLabel.Text = "0%";
                FbProgress.Progress = 0;
                WalkInProgress.Progress = 0;
                LowStockLabel.Text = "0";
                AvgOrderValueLabel.Text = "₱0";
                await DisplayAlert("Error", "Failed to load analytics. Check connection.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private void BuildWeeklyChart(List<decimal> sales, List<string> labels)
        {
            WeeklyChartGrid.Children.Clear();
            WeeklyChartGrid.RowDefinitions.Clear();
            WeeklyChartGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            WeeklyChartGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            if (sales.Count == 0) return;

            var maxSales = sales.Max();
            if (maxSales <= 0) maxSales = 1;

            for (int i = 0; i < 7 && i < sales.Count; i++)
            {
                var col = i;

                var barHeight = Math.Max(8, (double)(sales[i] / maxSales) * 60);

                var bar = new BoxView
                {
                    HeightRequest = barHeight,
                    WidthRequest = 28,
                    CornerRadius = 6,
                    Color = sales[i] > 0
                        ? (Color)Application.Current!.Resources["Primary"]
                        : (Color)Application.Current!.Resources["Gray300"],
                    VerticalOptions = LayoutOptions.End
                };

                var valueLabel = new Label
                {
                    Text = sales[i] > 0 ? $"₱{(int)sales[i] / 1000}k" : "",
                    FontSize = 8,
                    TextColor = (Color)Application.Current!.Resources["Gray500"],
                    HorizontalTextAlignment = TextAlignment.Center
                };

                var dayLabel = new Label
                {
                    Text = i < labels.Count ? labels[i] : "",
                    FontSize = 10,
                    TextColor = (Color)Application.Current!.Resources["Gray500"],
                    HorizontalTextAlignment = TextAlignment.Center
                };

                var stack = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 4
                };
                stack.Children.Add(bar);
                stack.Children.Add(valueLabel);
                stack.Children.Add(dayLabel);

                Grid.SetColumn(stack, col);
                Grid.SetRow(stack, 0);
                Grid.SetRowSpan(stack, 2);
                WeeklyChartGrid.Children.Add(stack);
            }
        }
    }
}
