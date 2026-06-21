using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class OrdersPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly ObservableCollection<OrderSummary> _allOrders = [];
        private string _activeFilter = "All";
        private string _searchText = "";

        public ObservableCollection<OrderSummary> FilteredOrders { get; set; } = [];
        public bool IsLoading { get; set; }
        public bool IsRefreshing { get; set; }
        public int PendingCount => _allOrders.Count(o => o.Status == OrderStatus.Pending);
        public int PreparingCount => _allOrders.Count(o => o.Status == OrderStatus.Creating);
        public int CompletedCount => _allOrders.Count(o => o.Status == OrderStatus.Completed);
        public int TotalOrders => _allOrders.Count;

        public OrdersPage(ApiService api)
        {
            InitializeComponent();
            BindingContext = this;
            _api = api;
            OrdersRefreshView.Refreshing += OnRefreshing;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsLoading = true;
                OnPropertyChanged(nameof(IsLoading));
            });

            try
            {
                var orders = await _api.GetOrdersAsync();

                _allOrders.Clear();
                foreach (var order in orders)
                    _allOrders.Add(order);

                ApplyFilter();
            }
            catch
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Error", "Failed to load orders. Check connection.", "OK");
                });
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    IsRefreshing = false;
                    OnPropertyChanged(nameof(IsLoading));
                    OnPropertyChanged(nameof(IsRefreshing));
                });
            }
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadOrdersAsync();
        }

        private void ApplyFilter()
        {
            var filtered = _allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var search = _searchText.ToLower();
                filtered = filtered.Where(o =>
                    o.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    o.OrderId.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            filtered = _activeFilter switch
            {
                "Pending" => filtered.Where(o => o.Status == OrderStatus.Pending),
                "Preparing" => filtered.Where(o => o.Status == OrderStatus.Creating),
                "Completed" => filtered.Where(o => o.Status == OrderStatus.Completed),
                _ => filtered
            };

            FilteredOrders.Clear();
            foreach (var order in filtered.OrderByDescending(o => o.IsJustReceived))
                FilteredOrders.Add(order);

            RefreshCounts();
        }

        private void RefreshCounts()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(PreparingCount));
                OnPropertyChanged(nameof(CompletedCount));
                OnPropertyChanged(nameof(TotalOrders));
            });
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue?.Trim() ?? "";
            ApplyFilter();
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            var parserPage = new OrderParserPage(_api);
            await Navigation.PushModalAsync(new NavigationPage(parserPage));
        }

        private async void OnNewOrderClicked(object sender, EventArgs e)
        {
            var formPage = new WalkInOrderFormPage(_api);
            formPage.OrderCreated += async (s, order) =>
            {
                await LoadOrdersAsync();
            };
            await Navigation.PushModalAsync(new NavigationPage(formPage));
        }

        private async void OnOrderActionClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var order = button?.BindingContext as OrderSummary;
            if (order == null) return;

            OrderStatus newStatus;
            switch (order.Status)
            {
                case OrderStatus.Pending:
                    newStatus = OrderStatus.Creating;
                    break;
                case OrderStatus.Creating:
                    newStatus = OrderStatus.Completed;
                    break;
                default:
                    return;
            }

            try
            {
                var success = await _api.UpdateOrderStatusAsync(order.OrderId, newStatus);
                if (success)
                {
                    order.Status = newStatus;
                    if (newStatus == OrderStatus.Completed)
                    {
                        await DisplayAlert("Order Complete", $"{order.OrderId} has been completed.", "OK");
                    }
                    ApplyFilter();
                }
            }
            catch
            {
                await DisplayAlert("Error", "Failed to update order. Check connection.", "OK");
            }
        }

        private async void OnCancelOrder(object sender, EventArgs e)
        {
            var item = sender as SwipeItem;
            var order = item?.BindingContext as OrderSummary;
            if (order == null) return;

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                await DisplayAlert("Cancel", "This order is already finished.", "OK");
                return;
            }

            var confirm = await DisplayAlert("Cancel Order",
                $"Cancel order {order.OrderId} for {order.CustomerName}?", "Yes, Cancel", "No");
            if (!confirm) return;

            try
            {
                var success = await _api.UpdateOrderStatusAsync(order.OrderId, OrderStatus.Cancelled);
                if (success)
                {
                    order.Status = OrderStatus.Cancelled;
                    ApplyFilter();
                }
            }
            catch
            {
                await DisplayAlert("Error", "Failed to cancel order. Check connection.", "OK");
            }
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            FilterAll.BackgroundColor = (Color)Application.Current!.Resources["Gray200"];
            FilterAll.TextColor = (Color)Application.Current!.Resources["Gray700"];
            FilterPending.BackgroundColor = (Color)Application.Current!.Resources["Gray200"];
            FilterPending.TextColor = (Color)Application.Current!.Resources["Gray700"];
            FilterPreparing.BackgroundColor = (Color)Application.Current!.Resources["Gray200"];
            FilterPreparing.TextColor = (Color)Application.Current!.Resources["Gray700"];

            button.BackgroundColor = (Color)Application.Current!.Resources["Primary"];
            button.TextColor = Colors.White;

            _activeFilter = button.Text;
            ApplyFilter();
        }
    }
}
