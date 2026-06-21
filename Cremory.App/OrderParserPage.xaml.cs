using Cremory.App.Services;

namespace Cremory.App
{
    public partial class OrderParserPage : ContentPage
    {
        private readonly ApiService _api;

        public OrderParserPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        private async void OnParseClicked(object sender, EventArgs e)
        {
            var rawText = RawTextEditor?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(rawText))
            {
                await DisplayAlert("Error", "Please paste an order message first.", "OK");
                return;
            }

            SubmitButton.IsEnabled = false;
            SubmitButton.Text = "Parsing...";
            ResultLabel.Text = "";

            try
            {
                var result = await _api.PostOrderParseAsync(rawText);
                if (result == null)
                {
                    ResultLabel.TextColor = Colors.Red;
                    ResultLabel.Text = "Failed to parse order. Check format.";
                }
                else
                {
                    ResultLabel.TextColor = Colors.Green;
                    ResultLabel.Text = $"Order created: {result.OrderId}";
                    RawTextEditor.Text = "";
                }
            }
            catch
            {
                ResultLabel.TextColor = Colors.Red;
                ResultLabel.Text = "Connection error. Check API.";
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                SubmitButton.Text = "Parse & Create Order";
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            ResultLabel.Text = "";
        }
    }
}
