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

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
