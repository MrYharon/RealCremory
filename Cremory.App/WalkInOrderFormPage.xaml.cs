using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class WalkInOrderFormPage : ContentPage
    {
        private readonly ApiService _api;

        public event EventHandler<OrderDto>? OrderCreated;

        public WalkInOrderFormPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
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
                await DisplayAlert("Validation", "Enter a valid total price.", "OK");
                return;
            }

            var contact = ContactEntry?.Text?.Trim();

            SaveButton.IsEnabled = false;
            SaveButton.Text = "Saving...";
            StatusLabel.Text = "";

            try
            {
                var result = await _api.CreateWalkInOrderAsync(name, items, price, contact);
                if (result != null)
                {
                    StatusLabel.TextColor = Colors.Green;
                    StatusLabel.Text = $"Order created: {result.OrderId}";
                    OrderCreated?.Invoke(this, result);
                    await Navigation.PopModalAsync();
                }
                else
                {
                    StatusLabel.TextColor = Colors.Red;
                    StatusLabel.Text = "Failed to create order. Check API.";
                }
            }
            catch
            {
                StatusLabel.TextColor = Colors.Red;
                StatusLabel.Text = "Connection error.";
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Text = "Create Order";
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
