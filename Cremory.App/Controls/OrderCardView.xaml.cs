using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App.Controls
{
    public partial class OrderCardView : ContentView
    {
        public OrderCardView()
        {
            InitializeComponent();
        }

        private OrderSummary? Order => BindingContext as OrderSummary;

        private async void OnCardTapped(object? sender, EventArgs e)
        {
            var order = Order;
            if (order == null) return;
            var page = GetCurrentPage();
            if (page == null) return;
            var detail = new OrderDetailPage(order, order.CreatedAtUtc, order.UpdatedAtUtc);
            await page.Navigation.PushModalAsync(new NavigationPage(detail));
        }

        private async void OnActionClicked(object sender, EventArgs e)
        {
            var order = Order;
            if (order == null) return;

            var next = OrderSummary.NextStatus(order.Status);
            if (next == null) return;

            try
            {
                var api = App.ApiService;
                if (api == null) return;
                var success = await api.UpdateOrderStatusAsync(order.OrderId, next.Value);

                if (success && next.Value == OrderStatus.Completed)
                {
                    var page = GetCurrentPage();
                    if (page != null)
                        await page.DisplayAlert("Order Complete", $"{order.OrderId} completed.", "OK");
                }
            }
            catch
            {
                var page = GetCurrentPage();
                if (page != null)
                    await page.DisplayAlert("Error", "Failed to update order.", "OK");
            }
        }

        private async void OnCancelSwipe(object sender, EventArgs e)
        {
            var order = Order;
            if (order == null) return;

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                return;

            var page = GetCurrentPage();
            if (page == null) return;

            var confirm = await page.DisplayAlert("Cancel Order",
                $"Cancel order {order.OrderId} for {order.CustomerName}?", "Yes, Cancel", "No");
            if (!confirm) return;

            try
            {
                var api = App.ApiService;
                if (api == null) return;
                await api.UpdateOrderStatusAsync(order.OrderId, OrderStatus.Cancelled);
            }
            catch
            {
                if (page != null)
                    await page.DisplayAlert("Error", "Failed to cancel order.", "OK");
            }
        }

        private async void OnEditSwipe(object sender, EventArgs e)
        {
            var order = Order;
            if (order == null) return;
            var page = GetCurrentPage();
            if (page == null) return;
            var editPage = new EditOrderPage(new OrderDto
            {
                OrderId = order.OrderId,
                CustomerName = order.CustomerName,
                CustomerContact = order.CustomerContact,
                Items = order.Items,
                TotalPrice = order.TotalPrice,
                Source = order.Source,
                Status = order.Status,
                CreatedAt = order.CreatedAtUtc,
                UpdatedAt = order.UpdatedAtUtc
            });
            await page.Navigation.PushModalAsync(new NavigationPage(editPage));
        }

        private async void OnDeleteSwipe(object sender, EventArgs e)
        {
            var order = Order;
            if (order == null) return;
            var page = GetCurrentPage();
            if (page == null) return;

            var confirm = await page.DisplayAlert("Delete Order",
                $"Permanently delete order {order.OrderId} for {order.CustomerName}?\nThis cannot be undone.",
                "Delete", "Cancel");
            if (!confirm) return;

            var api = App.ApiService;
            if (api == null) return;

            var (success, error) = await api.DeleteOrderAsync(order.OrderId);
            if (success)
                await page.DisplayAlert("Deleted", $"Order {order.OrderId} deleted.", "OK");
            else
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
