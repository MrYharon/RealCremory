using Android.App;
using Android.Content.PM;
using Android.OS;
using Firebase;
using Cremory.App.Services;

namespace Cremory.App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            InitializeFirebase();
            Services.NotificationService.CreateNotificationChannel();
            if (Window != null)
            {
                Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#C97586"));
            }
        }

        private static void InitializeFirebase()
        {
            if (FirebaseApp.GetApps(Platform.CurrentActivity ?? Android.App.Application.Context).Count == 0)
            {
                var options = new FirebaseOptions.Builder()
                    .SetApplicationId("1:438591289663:android:052886f97c17d046fabb8a")
                    .SetApiKey("AIzaSyCNSZa3vgNawcq1tZihQu-kUMihkAUR0Y4")
                    .SetProjectId("cremory-d58f8")
                    .Build();
                FirebaseApp.InitializeApp(Platform.CurrentActivity ?? Android.App.Application.Context, options);
            }
        }
    }
}
