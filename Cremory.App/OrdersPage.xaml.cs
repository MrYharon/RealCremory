using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public class KitchenItemGroup
    {
        public string ItemName { get; set; } = string.Empty;
        public int TotalQty { get; set; }
        public int OrderCount { get; set; }
    }

    public partial class OrdersPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly SignalRService _signalR;
        private readonly ObservableCollection<OrderSummary> _allOrders = [];
        private readonly ObservableCollection<KitchenItemGroup> _kitchenItems = [];
        private string _activeFilter = "Pending";
        private string? _deliveryTypeFilter;
        private string _searchText = "";
        private bool _showArchives;
        private CancellationTokenSource? _searchCts;
        private bool _signalRSubscribed;
        private int _currentPage = 1;
        private int _totalCount;
        private bool _hasMorePages = true;
        private bool _isLoadingMore;
        private bool _showKitchenView;

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
            KitchenCollectionView.ItemsSource = _kitchenItems;
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

        private async void OnOrderCreated(OrderDto dto)
        {
            var summary = OrderSummary.FromDto(dto);
            _allOrders.Add(summary);
            ApplyFilter();
            await ShowNewOrderAlert(summary);
        }

        private async Task ShowNewOrderAlert(OrderSummary order)
        {
            AlertTitle.Text = "New Order Received!";
            AlertMessage.Text = $"{order.CustomerName} — {order.Items}";
            NewOrderAlert.IsVisible = true;
            NewOrderAlert.Opacity = 0;
            await NewOrderAlert.FadeTo(1, 300, Easing.CubicOut);
            await Task.Delay(5000);
            await NewOrderAlert.FadeTo(0, 300, Easing.CubicIn);
            NewOrderAlert.IsVisible = false;
        }

        private void OnDismissAlert(object sender, TappedEventArgs e)
        {
            NewOrderAlert.IsVisible = false;
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
                SkeletonLoading.IsVisible = true;
                OrdersCollectionView.IsVisible = false;
                _ = AnimateSkeleton();
            });

            try
            {
                var connectTask = _signalR.EnsureStartedAsync();
                var timeoutTask = Task.Delay(5000);
                await Task.WhenAny(connectTask, timeoutTask);
                _currentPage = 1;
                _hasMorePages = true;

                var (status, dateFrom, dateTo, isArchived, deliveryType) = GetServerFilterParams();

                var (dtos, totalCount) = await _api.GetOrdersPagedAsync(
                    status: status, search: string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
                    dateFrom: dateFrom, dateTo: dateTo,
                    page: _currentPage, pageSize: 100,
                    isArchived: isArchived, deliveryType: deliveryType);

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
                    SkeletonLoading.IsVisible = false;
                    OrdersCollectionView.IsVisible = true;
                });
            }
        }

        private bool _skeletonAnimating;
        private async Task AnimateSkeleton()
        {
            if (_skeletonAnimating) return;
            _skeletonAnimating = true;
            while (SkeletonLoading.IsVisible)
            {
                await SkeletonLoading.FadeTo(0.4, 600, Easing.SinInOut);
                await SkeletonLoading.FadeTo(1.0, 600, Easing.SinInOut);
            }
            _skeletonAnimating = false;
        }

        private async Task LoadMoreOrdersAsync()
        {
            if (_isLoadingMore || !_hasMorePages) return;
            _isLoadingMore = true;

            try
            {
                _currentPage++;
                var (status, dateFrom, dateTo, isArchived, deliveryType) = GetServerFilterParams();

                var (dtos, totalCount) = await _api.GetOrdersPagedAsync(
                    status: status, search: string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
                    dateFrom: dateFrom, dateTo: dateTo,
                    page: _currentPage, pageSize: 100,
                    isArchived: isArchived, deliveryType: deliveryType);

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

        private (string? Status, DateTime? DateFrom, DateTime? DateTo, bool? IsArchived, string? DeliveryType) GetServerFilterParams()
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
                return (status, dateFrom, dateTo, true, null);
            }

            var activeStatus = _activeFilter switch
            {
                "Pending" => "Pending",
                "Preparing" => "Creating",
                "To Pay" => "ToPay",
                "Completed" => "Completed",
                "Cancelled" => "Cancelled",
                _ => null
            };
            var deliveryType = _activeFilter == "Completed" ? _deliveryTypeFilter : null;
            return (activeStatus, null, null, null, deliveryType);
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
                filtered = filtered.Where(o => o.IsArchived);

                var statusFilter = ArchiveStatusPicker.SelectedItem as string;
                if (statusFilter == "Completed")
                    filtered = filtered.Where(o => o.Status == OrderStatus.Completed);
                else if (statusFilter == "Cancelled")
                    filtered = filtered.Where(o => o.Status == OrderStatus.Cancelled);
            }
            else
            {
                filtered = filtered.Where(o => !o.IsArchived);

                if (_activeFilter != "All")
                {
                    filtered = _activeFilter switch
                    {
                        "Pending" => filtered.Where(o => o.Status == OrderStatus.Pending),
                        "Preparing" => filtered.Where(o => o.Status == OrderStatus.Creating),
                        "To Pay" => filtered.Where(o => o.Status == OrderStatus.ToPay),
                        "Completed" => filtered.Where(o => o.Status == OrderStatus.Completed),
                        "Cancelled" => filtered.Where(o => o.Status == OrderStatus.Cancelled),
                        _ => filtered
                    };
                }
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

        private async void OnToggleArchives(object sender, TappedEventArgs e)
        {
            _showArchives = !_showArchives;
            ArchiveFilterBar.IsVisible = _showArchives;
            CompletedFilterBar.IsVisible = false;
            ArchiveToggleBtn.BackgroundColor = _showArchives ? Color.FromArgb("#E27575") : Colors.Transparent;
            ArchiveToggleBtn.Stroke = _showArchives ? Colors.Transparent : Color.FromArgb("#E8D4D4");
            ArchiveLabel.TextColor = _showArchives ? Colors.White : Color.FromArgb("#B89292");
            ArchiveLabel.Text = _showArchives ? "Archives On" : "Archives";
            if (_showArchives)
                ResetFilterChips();
            await LoadOrdersAsync();
        }

        private async void OnDateFilterChanged(object sender, EventArgs e)
        {
            if (_showArchives)
                await LoadOrdersAsync();
        }

        private async void OnDeliveryTypeFilterChanged(object sender, EventArgs e)
        {
            var selected = DeliveryTypePicker.SelectedItem as string;
            _deliveryTypeFilter = selected is "All" or null ? null : selected;
            await LoadOrdersAsync();
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
            var borders = new[] { FilterPendingBorder, FilterPreparingBorder, FilterToPayBorder, FilterCompletedBorder, FilterCancelledBorder };
            var buttons = new[] { FilterPending, FilterPreparing, FilterToPay, FilterCompleted, FilterCancelled };
            for (int i = 0; i < borders.Length; i++)
            {
                borders[i].BackgroundColor = Colors.Transparent;
                borders[i].Stroke = (Color)Application.Current!.Resources["Gray300"];
                buttons[i].TextColor = (Color)Application.Current!.Resources["Gray600"];
                buttons[i].FontAttributes = FontAttributes.None;
            }
        }

        private async void OnFilterClicked(object sender, EventArgs e)
        {
            if (_showKitchenView)
            {
                ToggleKitchenView();
            }

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
            ArchiveToggleBtn.BackgroundColor = Colors.Transparent;
            ArchiveToggleBtn.Stroke = Color.FromArgb("#E8D4D4");
            ArchiveLabel.TextColor = Color.FromArgb("#B89292");
            ArchiveLabel.Text = "Archives";
            ArchiveFilterBar.IsVisible = false;
            CompletedFilterBar.IsVisible = _activeFilter == "Completed";
            if (_activeFilter != "Completed")
            {
                _deliveryTypeFilter = null;
                DeliveryTypePicker.SelectedIndex = -1;
            }

            await LoadOrdersAsync();
        }

        private void OnKitchenViewClicked(object sender, EventArgs e)
        {
            ToggleKitchenView();
        }

        private void ToggleKitchenView()
        {
            _showKitchenView = !_showKitchenView;

            if (_showKitchenView)
            {
                KitchenViewBtn.Text = "Orders View";
                KitchenViewBorder.BackgroundColor = (Color)Application.Current!.Resources["PrimaryDark"];
                KitchenViewBorder.Stroke = Colors.Transparent;
                KitchenViewBtn.TextColor = Colors.White;
                KitchenViewBtn.FontAttributes = FontAttributes.Bold;

                OrdersCollectionView.IsVisible = false;
                KitchenCollectionView.IsVisible = true;
                BuildKitchenAggregation();
            }
            else
            {
                KitchenViewBtn.Text = "Kitchen View";
                KitchenViewBorder.BackgroundColor = Colors.Transparent;
                KitchenViewBorder.Stroke = (Color)Application.Current!.Resources["Gray300"];
                KitchenViewBtn.TextColor = (Color)Application.Current!.Resources["Gray600"];
                KitchenViewBtn.FontAttributes = FontAttributes.None;

                OrdersCollectionView.IsVisible = true;
                KitchenCollectionView.IsVisible = false;
            }
        }

        private void BuildKitchenAggregation()
        {
            var pendingOrders = _allOrders
                .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Creating)
                .ToList();

            var itemCounts = new Dictionary<string, (int TotalQty, HashSet<string> OrderIds)>();

            foreach (var order in pendingOrders)
            {
                var items = ParseOrderItems(order.Items);
                foreach (var (name, qty) in items)
                {
                    if (!itemCounts.ContainsKey(name))
                        itemCounts[name] = (0, []);
                    var existing = itemCounts[name];
                    itemCounts[name] = (existing.TotalQty + qty, existing.OrderIds);
                    itemCounts[name].OrderIds.Add(order.OrderId);
                }
            }

            _kitchenItems.Clear();
            foreach (var kvp in itemCounts.OrderByDescending(x => x.Value.TotalQty))
            {
                _kitchenItems.Add(new KitchenItemGroup
                {
                    ItemName = kvp.Key,
                    TotalQty = kvp.Value.TotalQty,
                    OrderCount = kvp.Value.OrderIds.Count
                });
            }
        }

        private static List<(string Name, int Qty)> ParseOrderItems(string itemsText)
        {
            if (string.IsNullOrWhiteSpace(itemsText))
                return [];

            var result = new List<(string Name, int Qty)>();
            var lines = itemsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim().TrimStart('•', '-', '*');
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var match = Regex.Match(trimmed, @"^(\d+)\s*[xX]?\s*(.+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var qty))
                    result.Add((match.Groups[2].Value.Trim(), qty));
                else
                    result.Add((trimmed, 1));
            }

            return result;
        }
    }
}