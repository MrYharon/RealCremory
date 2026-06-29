using Cremory.App.Models;

namespace Cremory.App
{
    public partial class EditOrderPage : ContentPage
    {
        private readonly OrderDto _original;

        public EditOrderPage(OrderDto order)
        {
            InitializeComponent();
            _original = order;

            NameEntry.Text = order.CustomerName;
            ContactEntry.Text = order.CustomerContact;
            ItemsEditor.Text = order.Items;
            PriceEntry.Text = order.TotalPrice.ToString("F2");
            SourcePicker.SelectedIndex = order.Source == "Facebook" ? 1 : 0;
            StatusLabel.Text = $"Order #{order.OrderId} — {order.Status}";
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var name = NameEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Validation", "Customer name is required.", "OK");
                return;
            }

            var items = ItemsEditor?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(items))
            {
                await DisplayAlert("Validation", "Items are required.", "OK");
                return;
            }

            if (!decimal.TryParse(PriceEntry?.Text?.Trim(), out var price) || price <= 0)
            {
                await DisplayAlert("Validation", "Enter a valid total price greater than 0.", "OK");
                return;
            }

            var updated = new OrderDto
            {
                OrderId = _original.OrderId,
                CustomerName = name,
                CustomerContact = ContactEntry?.Text?.Trim(),
                Items = items,
                TotalPrice = price,
                Source = SourcePicker.SelectedItem as string ?? "Walk-in",
                Status = _original.Status,
                CreatedAt = _original.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            SaveButton.IsEnabled = false;
            SaveButton.Text = "Saving...";
            try
            {
                var api = App.ApiService;
                if (api == null) return;

                var (success, error) = await api.UpdateOrderAsync(updated);
                if (success)
                {
                    await DisplayAlert("Saved", "Order updated successfully.", "OK");
                    await Navigation.PopModalAsync();
                }
                else
                {
                    await DisplayAlert("Error", $"Failed to save: {error}", "OK");
                }
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Text = "Save Changes";
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
