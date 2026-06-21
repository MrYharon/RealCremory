using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class OrderArchivesPage : ContentPage
    {
        private readonly ApiService _api;
        private List<OrderSummary> _allArchivedOrders = [];
        private List<OrderDto> _allDtos = [];

        public OrderArchivesPage(ApiService api)
        {
            InitializeComponent();
            DateRangePicker.SelectedIndex = 3;
            StatusPicker.SelectedIndex = 0;
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                var dtos = await _api.GetOrdersAsync(direct: true);
                _allDtos = dtos;
                _allArchivedOrders = dtos
                    .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled)
                    .Select(OrderSummary.FromDto)
                    .ToList();

                ApplyFilters();
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load archived orders. Check connection.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allDtos.AsEnumerable();
            var now = DateTime.UtcNow;

            var dateFilter = DateRangePicker.SelectedItem as string;
            if (dateFilter == "Today")
                filtered = filtered.Where(o => o.CreatedAt.Date == now.Date);
            else if (dateFilter == "This Week")
                filtered = filtered.Where(o => o.CreatedAt > now.AddDays(-7));
            else if (dateFilter == "This Month")
                filtered = filtered.Where(o => o.CreatedAt > now.AddDays(-30));

            var statusFilter = StatusPicker.SelectedItem as string;
            if (statusFilter == "Completed")
                filtered = filtered.Where(o => o.Status == OrderStatus.Completed);
            else if (statusFilter == "Cancelled")
                filtered = filtered.Where(o => o.Status == OrderStatus.Cancelled);
            else
                filtered = filtered.Where(o => o.Status == OrderStatus.Completed
                                               || o.Status == OrderStatus.Cancelled);

            ArchivesCollectionView.ItemsSource = filtered
                .Select(OrderSummary.FromDto)
                .OrderByDescending(o => o.Timestamp)
                .ToList();
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }
    }
}
