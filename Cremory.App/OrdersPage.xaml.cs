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
        private int _currentPage = 1;
        private int _totalCount;
        private bool _hasMorePages = true;
        private bool _isLoadingMore;

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
                _currentPage = 1;
                _hasMorePages = true;

                var (status, dateFrom, dateTo) = GetServerFilterParams();

                var (dtos, totalCount) = await _api.GetOrdersPagedAsync(
                    status: status, search: string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
                    dateFrom: dateFrom, dateTo: dateTo,
                    page: _currentPage, pageSize: 100);

                _totalCount = totalCount;
                _hasMorePages = dtos.Count >= 100;

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

        private async Task LoadMoreOrdersAsync()
        {
            if (_isLoadingMore || !_hasMorePages) return;
            _isLoadingMore = true;

            try
            {
                _currentPage++;
                var (status, dateFrom, dateTo) = GetServerFilterParams();

                var (dtos, totalCount) = await _api.GetOrdersPagedAsync(
                    status: status, search: string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
                    dateFrom: dateFrom, dateTo: dateTo,
                    page: _currentPage, pageSize: 100);

                _totalCount = totalCount;
                _hasMorePages = dtos.Count >= 100;

                var orders = dtos.Select(OrderSummary.FromDto).ToList();
                foreach (var order in orders)
                {
                    if (!_allOrders.Any(o => o.OrderId == order.OrderId))
                        _allOrders.Add(order);
                }

                ApplyFilter();
            }
            catch { }
            finally
            {
                _isLoadingMore = false;
            }
        }

        private (string? Status, DateTime? DateFrom, DateTime? DateTo) GetServerFilterParams()
        {
            if (_showArchives)
            {
                var dateFilter = DateRangePicker.SelectedItem as string;
                DateTime? dateFrom = null;
                DateTime? dateTo = null;
                var now = DateTime.UtcNow;

                if (dateFilter == "Today") dateFrom = now.Date;
                else if (dateFilter == "This Week") dateFrom = now.AddDays(-7);
                else if (dateFilter == "This Month") dateFrom = now.AddDays(-30);

                var statusFilter = ArchiveStatusPicker.SelectedItem as string;
                var status = statusFilter switch
                {
                    "Completed" => "Completed",
                    "Cancelled" => "Cancelled",
                    _ => null
                };
                return (status, dateFrom, dateTo);
            }

            var activeStatus = _activeFilter switch
            {
                "Pending" => "Pending",
                "Preparing" => "Creating",
                "Completed" => "Completed",
                _ => null
            };
            return (activeStatus, null, null);
        }

        private async void OnLoadMore(object sender, EventArgs e)
        {
            await LoadMoreOrdersAsync();
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
                await LoadOrdersAsync();
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void OnToggleArchives(object sender, ToggledEventArgs e)
        {
            _showArchives = e.Value;
            ArchiveFilterBar.IsVisible = _showArchives;
            if (_showArchives)
            {
                ResetFilterChips();
            }
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

        private void ResetFilterChips()
        {
            var borders = new[] { FilterAllBorder, FilterPendingBorder, FilterPreparingBorder, FilterCompletedBorder };
            var buttons = new[] { FilterAll, FilterPending, FilterPreparing, FilterCompleted };
            for (int i = 0; i < borders.Length; i++)
            {
                borders[i].BackgroundColor = Colors.Transparent;
                borders[i].Stroke = (Color)Application.Current!.Resources["Gray300"];
                buttons[i].TextColor = (Color)Application.Current!.Resources["Gray600"];
                buttons[i].FontAttributes = FontAttributes.None;
            }
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            ResetFilterChips();

            var border = button.Parent as Border;
            if (border != null)
            {
                border.BackgroundColor = (Color)Application.Current!.Resources["Primary"];
                border.Stroke = Colors.Transparent;
            }
            button.TextColor = Colors.White;
            button.FontAttributes = FontAttributes.Bold;

            _activeFilter = button.Text;
            _showArchives = false;
            ArchiveToggle.IsToggled = false;
            ArchiveFilterBar.IsVisible = false;
            ApplyFilter();
        }
    }
}
