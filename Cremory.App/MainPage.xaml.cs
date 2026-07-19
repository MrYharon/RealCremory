using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class MainPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly SignalRService _signalR;
        private readonly List<OrderSummary> _allOrders = [];

        public ObservableCollection<OrderSummary> ActiveOrders { get; set; } = [];
        public decimal TotalProfitToday { get; set; }
        public int TotalOrdersToday { get; set; }
        public bool IsLoading { get; set; }
        public string CompletedOrdersText { get; set; } = "";
        public string OrdersTotalText { get; set; } = "";

        private DateTime _lastRefreshTime = DateTime.Now;

        public MainPage(ApiService api, SignalRService signalR)
        {
            InitializeComponent();
            BindingContext = this;
            _api = api;
            _signalR = signalR;
        }

        private IDispatcherTimer? _refreshTimer;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOrdersAsync();
            SubscribeToSignalR();
            await _signalR.EnsureStartedAsync();
            StartRefreshTimer();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _refreshTimer?.Stop();
            UnsubscribeFromSignalR();
        }

        private void StartRefreshTimer()
        {
            _refreshTimer = Dispatcher.CreateTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(30);
            _refreshTimer.Tick += (s, e) =>
            {
                _lastRefreshTime = DateTime.Now;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LastRefreshLabel.Text = "Just now";
                });
            };
            _refreshTimer.Start();
        }

        private void SubscribeToSignalR()
        {
            _signalR.OrderCreated += OnOrderCreated;
            _signalR.OrderUpdated += OnOrderUpdated;
            _signalR.OrderDeleted += OnOrderDeleted;
        }

        private void UnsubscribeFromSignalR()
        {
            _signalR.OrderCreated -= OnOrderCreated;
            _signalR.OrderUpdated -= OnOrderUpdated;
            _signalR.OrderDeleted -= OnOrderDeleted;
        }

        private void OnOrderCreated(OrderDto dto)
        {
            var summary = OrderSummary.FromDto(dto);
            _allOrders.Add(summary);
            if (summary.Status != OrderStatus.Completed && summary.Status != OrderStatus.Cancelled)
                ActiveOrders.Insert(0, summary);
            RefreshTotals();
        }

        private void OnOrderUpdated(OrderDto dto)
        {
            var updated = OrderSummary.FromDto(dto);
            var allIdx = _allOrders.FindIndex(o => o.OrderId == dto.OrderId);
            if (allIdx >= 0)
                _allOrders[allIdx] = updated;
            else
                _allOrders.Add(updated);

            var existing = ActiveOrders.FirstOrDefault(o => o.OrderId == dto.OrderId);
            if (existing != null)
            {
                var index = ActiveOrders.IndexOf(existing);
                if (updated.Status == OrderStatus.Completed || updated.Status == OrderStatus.Cancelled)
                    ActiveOrders.RemoveAt(index);
                else
                    ActiveOrders[index] = updated;
            }
            else if (dto.Status != OrderStatus.Completed && dto.Status != OrderStatus.Cancelled)
            {
                ActiveOrders.Add(updated);
            }
            RefreshTotals();
        }

        private void OnOrderDeleted(string orderId)
        {
            _allOrders.RemoveAll(o => o.OrderId == orderId);
            var existing = ActiveOrders.FirstOrDefault(o => o.OrderId == orderId);
            if (existing != null)
            {
                ActiveOrders.Remove(existing);
                RefreshTotals();
            }
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadOrdersAsync();
            MainThread.BeginInvokeOnMainThread(() => MainRefreshView.IsRefreshing = false);
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
                var active = orders.Where(o => o.Status != OrderStatus.Completed
                                               && o.Status != OrderStatus.Cancelled).ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _allOrders.Clear();
                    _allOrders.AddRange(orders);
                    ActiveOrders.Clear();
                    foreach (var order in active)
                        ActiveOrders.Add(order);

                    RefreshTotals();
                });

                _ = LoadLowStockAsync();
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

        private async Task LoadLowStockAsync()
        {
            try
            {
                var lowStock = await _api.GetLowStockProductsAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (lowStock.Count > 0)
                    {
                        LowStockBanner.IsVisible = true;
                        LowStockTitle.Text = $"Low Stock Alert ({lowStock.Count})";
                        LowStockCount.Text = string.Join(", ", lowStock.Take(3).Select(p =>
                            $"{p.Name} ({p.CurrentStock}/{p.LowStockThreshold})"));
                        if (lowStock.Count > 3)
                            LowStockCount.Text += $" and {lowStock.Count - 3} more";
                    }
                    else
                    {
                        LowStockBanner.IsVisible = false;
                    }
                });
            }
            catch { }
        }

        private async void OnViewLowStockClicked(object sender, EventArgs e)
        {
            var shell = Shell.Current;
            if (shell != null)
                await shell.GoToAsync("//SettingsPage");
        }

        private void RefreshTotals()
        {
            var completed = _allOrders.Where(o => o.Status == OrderStatus.Completed).ToList();
            TotalProfitToday = completed.Sum(o => o.TotalPrice);
            TotalOrdersToday = _allOrders.Count;
            CompletedOrdersText = $"{completed.Count} orders completed";
            OrdersTotalText = $"₱{_allOrders.Sum(o => o.TotalPrice):N0} total";

            var pendingCount = _allOrders.Count(o => o.Status == OrderStatus.Pending);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var flyoutItems = Shell.Current?.Items;
                    if (flyoutItems != null)
                    {
                        foreach (var item in flyoutItems)
                        {
                            if (item.Title == "Orders" && item.Items.Count > 0 && item.Items[0].Items.Count > 0)
                            {
                                var content = item.Items[0].Items[0];
                                var badgeProp = content.GetType().GetProperty("Badge");
                                if (badgeProp != null)
                                    badgeProp.SetValue(content, pendingCount > 0 ? pendingCount.ToString() : null);
                                break;
                            }
                        }
                    }
                }
                catch { }
            });

            OnPropertyChanged(nameof(ActiveOrders));
            OnPropertyChanged(nameof(TotalProfitToday));
            OnPropertyChanged(nameof(TotalOrdersToday));
            OnPropertyChanged(nameof(CompletedOrdersText));
            OnPropertyChanged(nameof(OrdersTotalText));
        }

    }
}
