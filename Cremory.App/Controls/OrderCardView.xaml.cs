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

        private async void OnCardDoubleTapped(object? sender, EventArgs e)
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

                if (success)
                {
                    order.Status = next.Value;
                    CardBorder.Stroke = Color.FromArgb(order.BorderColor);
                    await CardBorder.FadeTo(0.85, 100, Easing.CubicOut);
                    await CardBorder.FadeTo(1.0, 200, Easing.CubicIn);
                }
            }
            catch
            {
            }
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

                if (success)
                {
                    order.Status = next.Value;
                    CardBorder.Stroke = Color.FromArgb(order.BorderColor);
                    await CardBorder.FadeTo(0.85, 100, Easing.CubicOut);
                    await CardBorder.FadeTo(1.0, 200, Easing.CubicIn);
                }
            }
            catch
            {
            }
        }

        private async void OnMoreClicked(object sender, EventArgs e)
        {
            var order = Order;
            if (order == null) return;
            var page = GetCurrentPage();
            if (page == null) return;

            var action = await page.DisplayActionSheet(
                $"Order {order.OrderId}",
                "Close", null,
                "Edit",
                order.Status != OrderStatus.Completed && order.Status != OrderStatus.Cancelled ? "Cancel" : null,
                "Delete"
            );

            switch (action)
            {
                case "Edit":
                    await OpenEditPage(order, page);
                    break;
                case "Cancel":
                    await CancelOrder(order, page);
                    break;
                case "Delete":
                    await DeleteOrder(order, page);
                    break;
            }
        }

        private async Task OpenEditPage(OrderSummary order, Page page)
        {
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

        private async Task CancelOrder(OrderSummary order, Page page)
        {
            var confirm = await page.DisplayAlert("Cancel Order",
                $"Cancel order for {order.CustomerName}?", "Yes, Cancel", "No");
            if (!confirm) return;

            try
            {
                var api = App.ApiService;
                if (api == null) return;
                await api.UpdateOrderStatusAsync(order.OrderId, OrderStatus.Cancelled);
            }
            catch
            {
                await page.DisplayAlert("Error", "Failed to cancel order.", "OK");
            }
        }

        private async Task DeleteOrder(OrderSummary order, Page page)
        {
            var confirm = await page.DisplayAlert("Delete Order",
                $"Permanently delete order for {order.CustomerName}?\nThis cannot be undone.",
                "Delete", "Cancel");
            if (!confirm) return;

            var api = App.ApiService;
            if (api == null) return;

            var (success, error) = await api.DeleteOrderAsync(order.OrderId);
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
