using Cremory.App.Services;

namespace Cremory.App
{
    public partial class LoginPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly IBiometricAuthService _biometricAuth;

        public LoginPage(ApiService api, IBiometricAuthService biometricAuth)
        {
            InitializeComponent();
            _api = api;
            _biometricAuth = biometricAuth;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(300);
                await TryBiometricAsync();
            });
        }

        private async Task TryBiometricAsync()
        {
            try
            {
                var ok = await _biometricAuth.AuthenticateAsync("Scan fingerprint to unlock Cremory");
                if (ok)
                    await NavigateToAppAsync();
            }
            catch { }
        }

        private async void OnPasswordCompleted(object? sender, EventArgs e)
        {
            await DoLoginAsync();
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            await DoLoginAsync();
        }

        private async Task DoLoginAsync()
        {
            var username = UsernameEntry?.Text?.Trim();
            var password = PasswordEntry?.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Enter username and password.");
                return;
            }

            LoginBtn.IsEnabled = false;
            LoginBtn.Text = "Signing in...";
            ErrorLabel.IsVisible = false;

            try
            {
                var result = await _api.LoginAsync(username, password);
                if (result != null)
                    await NavigateToAppAsync();
                else
                    ShowError("Invalid username or password.");
            }
            catch
            {
                ShowError("Connection error. Check network.");
            }
            finally
            {
                LoginBtn.IsEnabled = true;
                LoginBtn.Text = "Sign In";
            }
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }

        private async Task NavigateToAppAsync()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window != null)
                window.Page = new AppShell();
        }
    }
}
