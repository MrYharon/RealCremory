using Cremory.App.Services;

namespace Cremory.App
{
    public partial class App : Application
    {
        public static ApiService? ApiService { get; private set; }
        private readonly IServiceProvider _serviceProvider;

        public App(ApiService apiService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            ApiService = apiService;
            _serviceProvider = serviceProvider;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
            return new Window(loginPage);
        }

        protected override void OnStart()
        {
            base.OnStart();
            RequestNotificationPermission();
        }

        private static void RequestNotificationPermission()
        {
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Tiramisu)
                return;

            var activity = Platform.CurrentActivity;
            if (activity == null) return;

            if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(activity, Android.Manifest.Permission.PostNotifications)
                != Android.Content.PM.Permission.Granted)
            {
                AndroidX.Core.App.ActivityCompat.RequestPermissions(activity, new[] { Android.Manifest.Permission.PostNotifications }, 0);
            }
#endif
        }
    }
}
