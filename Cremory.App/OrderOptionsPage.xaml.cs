using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class OrderOptionsPage : ContentPage
    {
        private readonly OrderSummary _order;
        private readonly ApiService _api;

        public OrderOptionsPage(OrderSummary order, ApiService api)
        {
            InitializeComponent();
            _order = order;
            _api = api;
            OrderTitleLabel.Text = $"{order.CustomerName} · {order.OrderId}";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Sheet.TranslateTo(0, 300, 0);
            await Sheet.TranslateTo(0, 0, 250, Easing.CubicOut);
        }

        private async void OnBackgroundTapped(object? sender, TappedEventArgs e)
        {
            await Dismiss();
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await Dismiss();
        }

        private async Task Dismiss()
        {
            await Sheet.TranslateTo(0, 300, 200, Easing.CubicIn);
            await Navigation.PopModalAsync(animated: false);
        }

        private async void OnEditTapped(object? sender, TappedEventArgs e)
        {
            await Dismiss();
            var page = GetCurrentPage();
            if (page == null) return;
            var editPage = new EditOrderPage(new OrderDto
            {
                OrderId = _order.OrderId,
                CustomerName = _order.CustomerName,
                CustomerContact = _order.CustomerContact,
                Items = _order.Items,
                TotalPrice = _order.TotalPrice,
                Source = _order.Source,
                Status = _order.Status,
                CreatedAt = _order.CreatedAtUtc,
                UpdatedAt = _order.UpdatedAtUtc
            });
            await page.Navigation.PushModalAsync(new NavigationPage(editPage));
        }

        private async void OnCancelTapped(object? sender, TappedEventArgs e)
        {
            if (_order.Status == OrderStatus.Completed || _order.Status == OrderStatus.Cancelled)
            {
                await Dismiss();
                return;
            }

            await Dismiss();
            var page = GetCurrentPage();
            if (page == null) return;

            var confirm = await page.DisplayAlert("Cancel Order",
                $"Cancel order for {_order.CustomerName}?", "Yes, Cancel", "No");
            if (!confirm) return;

            try
            {
                await _api.UpdateOrderStatusAsync(_order.OrderId, OrderStatus.Cancelled);
            }
            catch
            {
                await page.DisplayAlert("Error", "Failed to cancel order.", "OK");
            }
        }

        private async void OnDeleteTapped(object? sender, TappedEventArgs e)
        {
            await Dismiss();
            var page = GetCurrentPage();
            if (page == null) return;

            var confirm = await page.DisplayAlert("Delete Order",
                $"Permanently delete order for {_order.CustomerName}?\nThis cannot be undone.",
                "Delete", "Cancel");
            if (!confirm) return;

            var (success, error) = await _api.DeleteOrderAsync(_order.OrderId);
            if (!success)
                await page.DisplayAlert("Error", $"Failed to delete: {error}", "OK");
        }

        private static Page? GetCurrentPage()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is Shell shell)
                return shell.CurrentPage ?? shell;
            return window?.Page;
        }
    }
}
