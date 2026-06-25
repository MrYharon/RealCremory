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
            await LoadAllData();
        }

        private async void OnRefreshing(object? sender, EventArgs e)
        {
            await LoadAllData();
            AnalyticsRefreshView.IsRefreshing = false;
        }

        private async Task LoadAllData()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var dashboardTask = _api.GetDashboardAnalyticsAsync();
                var financeTask = _api.GetFinanceSummaryAsync();

                await Task.WhenAll(dashboardTask, financeTask);

                var dashboard = await dashboardTask;
                var finance = await financeTask;

                if (dashboard != null)
                {
                    PopularItemsCollection.ItemsSource = dashboard.PopularItems;

                    FbPctLabel.Text = $"{dashboard.FacebookPct}%";
                    WalkInPctLabel.Text = $"{dashboard.WalkInPct}%";
                    FbProgress.Progress = dashboard.FacebookPct / 100.0;
                    WalkInProgress.Progress = dashboard.WalkInPct / 100.0;

                    LowStockLabel.Text = dashboard.LowStockCount.ToString();
                }

                if (finance != null)
                {
                    TodayRevenue.Text = $"₱{finance.TodayRevenue:N2}";
                    TodayOrdersLabel.Text = $"{finance.TodayOrders} orders";

                    WeekRevenue.Text = $"₱{finance.WeekRevenue:N2}";
                    WeekAverageLabel.Text = $"₱{finance.WeekAverage:N2}/order avg";

                    MonthRevenue.Text = $"₱{finance.MonthRevenue:N2}";
                    MonthOrdersLabel.Text = $"{finance.TotalOrdersMonth} orders this month";

                    AvgOrderLabel.Text = $"₱{finance.AvgOrderValue:N2}";
                    ProfitMarginLabel.Text = $"{finance.ProfitMargin}%";
                    NetProfitLabel.Text = $"₱{finance.NetProfit:N0}";

                    TransactionsCollection.ItemsSource = finance.RecentTransactions;
                }

                if (dashboard != null)
                    BuildWeeklyChart(dashboard.WeeklySales, dashboard.WeekLabels);
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load analytics data. Check connection.", "OK");
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
