using Cremory.App.Services;

namespace Cremory.App
{
    public partial class SettingsPage : ContentPage
    {
        private readonly ApiService _api;

        public SettingsPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var enabled = await _api.GetAutoDeductEnabledAsync();
                AutoDeductSwitch.IsToggled = enabled;
                AutoDeductStatus.Text = enabled ? "Stock deduction is ON" : "Stock deduction is OFF";
            }
            catch
            {
                AutoDeductStatus.Text = "Failed to load setting";
            }
        }

        private async void OnAutoDeductToggled(object sender, ToggledEventArgs e)
        {
            var success = await _api.SetAutoDeductEnabledAsync(e.Value);
            AutoDeductStatus.Text = success
                ? (e.Value ? "Stock deduction is ON" : "Stock deduction is OFF")
                : "Failed to save setting";
        }
    }
}
