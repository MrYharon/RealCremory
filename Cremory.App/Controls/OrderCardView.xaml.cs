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
                    UpdateStatus(order, next.Value);
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
                    UpdateStatus(order, next.Value);
            }
            catch
            {
            }
        }

        private void UpdateStatus(OrderSummary order, OrderStatus newStatus)
        {
            order.Status = newStatus;
            CardBorder.Stroke = Color.FromArgb(order.BorderColor);
            _ = AnimateFlash(CardBorder);
        }

        private static async Task AnimateFlash(View view)
        {
            await view.FadeTo(0.85, 100, Easing.CubicOut);
            await view.FadeTo(1.0, 200, Easing.CubicIn);
        }

        private async void OnMoreClicked(object sender, EventArgs e)
        {
            var order = Order;
            if (order == null) return;
            var page = GetCurrentPage();
            if (page == null) return;

            var optionsPage = new OrderOptionsPage(order, App.ApiService!);
            await page.Navigation.PushModalAsync(optionsPage);
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
