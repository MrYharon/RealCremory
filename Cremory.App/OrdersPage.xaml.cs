using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class OrdersPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly SignalRService _signalR;
        private readonly ObservableCollection<OrderSummary> _allOrders = [];
        private string _activeFilter = "All";
        private string _searchText = "";
        private bool _showArchives;
        private CancellationTokenSource? _searchCts;
        private bool _signalRSubscribed;

        public ObservableCollection<OrderSummary> FilteredOrders { get; set; } = [];
        public bool IsLoading { get; set; }
        public int PendingCount => _allOrders.Count(o => o.Status == OrderStatus.Pending);
        public int PreparingCount => _allOrders.Count(o => o.Status == OrderStatus.Creating);
        public int CompletedCount => _allOrders.Count(o => o.Status == OrderStatus.Completed);
        public int TotalOrders => _allOrders.Count;

        public OrdersPage(ApiService api, SignalRService signalR)
        {
            InitializeComponent();
            BindingContext = this;
            _api = api;
            _signalR = signalR;
            DateRangePicker.SelectedIndex = 3;
            ArchiveStatusPicker.SelectedIndex = 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOrdersAsync();
            SubscribeToSignalR();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            UnsubscribeFromSignalR();
        }

        private void SubscribeToSignalR()
        {
            if (_signalRSubscribed) return;
            _signalRSubscribed = true;
            _signalR.OrderCreated += OnOrderCreated;
            _signalR.OrderUpdated += OnOrderUpdated;
            _signalR.OrderDeleted += OnOrderDeleted;
        }

        private void UnsubscribeFromSignalR()
        {
            if (!_signalRSubscribed) return;
            _signalRSubscribed = false;
            _signalR.OrderCreated -= OnOrderCreated;
            _signalR.OrderUpdated -= OnOrderUpdated;
            _signalR.OrderDeleted -= OnOrderDeleted;
        }

        private void OnOrderCreated(OrderDto dto)
        {
            var summary = OrderSummary.FromDto(dto);
            _allOrders.Add(summary);
            ApplyFilter();
        }

        private void OnOrderUpdated(OrderDto dto)
        {
            var updated = OrderSummary.FromDto(dto);
            var existing = _allOrders.FirstOrDefault(o => o.OrderId == dto.OrderId);
            if (existing != null)
            {
                var idx = _allOrders.IndexOf(existing);
                _allOrders[idx] = updated;
            }
            else
            {
                _allOrders.Add(updated);
            }
            ApplyFilter();
        }

        private void OnOrderDeleted(string orderId)
        {
            var existing = _allOrders.FirstOrDefault(o => o.OrderId == orderId);
            if (existing != null)
                _allOrders.Remove(existing);
            ApplyFilter();
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
                await _signalR.EnsureStartedAsync();
                var dtos = await _api.GetOrdersRawAsync();
                var orders = dtos.Select(OrderSummary.FromDto).ToList();

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
                    OnPropertyChanged(nameof(IsLoading));
                });
            }
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadOrdersAsync();
            var rv = sender as RefreshView;
            if (rv != null) rv.IsRefreshing = false;
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

            if (_showArchives)
            {
                filtered = filtered.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled);

                var statusFilter = ArchiveStatusPicker.SelectedItem as string;
                if (statusFilter == "Completed")
                    filtered = filtered.Where(o => o.Status == OrderStatus.Completed);
                else if (statusFilter == "Cancelled")
                    filtered = filtered.Where(o => o.Status == OrderStatus.Cancelled);
            }
            else
            {
                filtered = _activeFilter switch
                {
                    "Pending" => filtered.Where(o => o.Status == OrderStatus.Pending),
                    "Preparing" => filtered.Where(o => o.Status == OrderStatus.Creating),
                    "Completed" => filtered.Where(o => o.Status == OrderStatus.Completed),
                    _ => filtered.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                };
            }

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

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token);
                _searchText = e.NewTextValue?.Trim() ?? "";
                ApplyFilter();
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void OnToggleArchives(object sender, EventArgs e)
        {
            _showArchives = !_showArchives;
            ArchiveFilterBar.IsVisible = _showArchives;
            ArchiveToggle.Text = _showArchives ? "Hide Archives" : "Show Archives";
            ArchiveToggle.BackgroundColor = _showArchives
                ? (Color)Application.Current!.Resources["Primary"]
                : (Color)Application.Current!.Resources["Gray200"];
            ArchiveToggle.TextColor = _showArchives
                ? Colors.White
                : (Color)Application.Current!.Resources["Gray700"];
            ApplyFilter();
        }

        private void OnDateFilterChanged(object sender, EventArgs e)
        {
            if (_showArchives)
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

        private async void OnCopyTemplate(object sender, EventArgs e)
        {
            var template = "Customer:\nItems:\nTotal:\nContact:\nOrder:";
            await Clipboard.Default.SetTextAsync(template);
            var btn = (Button)sender;
            btn.Text = "Copied!";
            await Task.Delay(1500);
            btn.Text = "Copy";
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var buttons = new[] { FilterAll, FilterPending, FilterPreparing, FilterCompleted };
            foreach (var btn in buttons)
            {
                btn.BackgroundColor = (Color)Application.Current!.Resources["Gray200"];
                btn.TextColor = (Color)Application.Current!.Resources["Gray700"];
            }

            button.BackgroundColor = (Color)Application.Current!.Resources["Primary"];
            button.TextColor = Colors.White;

            _activeFilter = button.Text;
            _showArchives = false;
            ArchiveFilterBar.IsVisible = false;
            ArchiveToggle.Text = "Show Archives";
            ArchiveToggle.BackgroundColor = (Color)Application.Current!.Resources["Gray200"];
            ArchiveToggle.TextColor = (Color)Application.Current!.Resources["Gray700"];
            ApplyFilter();
        }
    }
}
