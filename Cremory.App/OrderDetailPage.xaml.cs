using Cremory.App.Models;

namespace Cremory.App
{
    public partial class OrderDetailPage : ContentPage
    {
        public OrderSummary Order { get; }
        public string CreatedAt { get; }
        public string UpdatedAt { get; }
        public bool HasContact => !string.IsNullOrWhiteSpace(Order.CustomerContact);

        public OrderDetailPage(OrderDto dto) : this(OrderSummary.FromDto(dto), dto.CreatedAt, dto.UpdatedAt) { }

        public OrderDetailPage(OrderSummary order, DateTime createdAt, DateTime updatedAt)
        {
            InitializeComponent();
            Order = order;
            CreatedAt = createdAt.ToString("MMM dd, yyyy h:mm tt");
            UpdatedAt = updatedAt.ToString("MMM dd, yyyy h:mm tt");
            BindingContext = this;
        }

        private async void OnActionClicked(object sender, EventArgs e)
        {
            var next = OrderSummary.NextStatus(Order.Status);
            if (next == null) return;

            var api = App.ApiService;
            if (api == null) return;

            var success = await api.UpdateOrderStatusAsync(Order.OrderId, next.Value);
            if (success && next.Value == OrderStatus.Completed)
                await DisplayAlert("Complete", $"{Order.OrderId} marked complete.", "OK");

            await Navigation.PopModalAsync();
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Delete Order",
                $"Permanently delete order {Order.OrderId} for {Order.CustomerName}?\nThis cannot be undone.",
                "Delete", "Cancel");
            if (!confirm) return;

            var api = App.ApiService;
            if (api == null) return;

            var (success, error) = await api.DeleteOrderAsync(Order.OrderId);
            if (success)
            {
                await DisplayAlert("Deleted", $"Order {Order.OrderId} deleted.", "OK");
                await Navigation.PopModalAsync();
            }
            else
            {
                await DisplayAlert("Error", $"Failed to delete: {error}", "OK");
            }
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            var editPage = new EditOrderPage(new OrderDto
            {
                OrderId = Order.OrderId,
                CustomerName = Order.CustomerName,
                CustomerContact = Order.CustomerContact,
                Items = Order.Items,
                TotalPrice = Order.TotalPrice,
                Source = Order.Source,
                Status = Order.Status,
                CreatedAt = Order.CreatedAtUtc,
                UpdatedAt = Order.UpdatedAtUtc
            });
            await Navigation.PushModalAsync(new NavigationPage(editPage));
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
