using Cremory.App.Services;

namespace Cremory.App
{
    public partial class FinancesPage : ContentPage
    {
        private readonly ApiService _api;

        public FinancesPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadFinanceData();
        }

        private async Task LoadFinanceData()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var data = await _api.GetFinanceSummaryAsync();
                if (data == null) return;

                TodayRevenue.Text = $"₱{data.TodayRevenue:N2}";
                TodayOrdersLabel.Text = $"{data.TodayOrders} orders";

                WeekRevenue.Text = $"₱{data.WeekRevenue:N2}";
                WeekAverageLabel.Text = $"₱{data.WeekAverage:N2}/order avg";

                MonthRevenue.Text = $"₱{data.MonthRevenue:N2}";
                MonthOrdersLabel.Text = $"{data.TotalOrdersMonth} orders this month";

                AvgOrderLabel.Text = $"₱{data.AvgOrderValue:N2}";
                ProfitMarginLabel.Text = $"{data.ProfitMargin}%";
                NetProfitLabel.Text = $"₱{data.NetProfit:N0}";

                TransactionsCollection.ItemsSource = data.RecentTransactions;
            }
            catch (Exception ex)
            {
                TodayRevenue.Text = "₱0.00";
                WeekRevenue.Text = "₱0.00";
                MonthRevenue.Text = "₱0.00";
                AvgOrderLabel.Text = "₱0.00";
                ProfitMarginLabel.Text = "0%";
                NetProfitLabel.Text = "₱0";
                await DisplayAlert("Error", "Failed to load financial data. Check connection.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }
    }
}
