using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Firebase.Messaging;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App.Platforms.Android
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class CremoryFirebaseMessagingService : FirebaseMessagingService
    {
        public override void OnNewToken(string? token)
        {
            base.OnNewToken(token);
            if (!string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine($"FCM Token: {token}");
                FcmRegistrationService.CurrentToken = token;
                _ = FcmRegistrationService.RegisterTokenAsync(token);
            }
        }

        public override void OnMessageReceived(RemoteMessage? message)
        {
            base.OnMessageReceived(message);
            if (message?.GetNotification() != null)
            {
                ShowNotification(
                    message.GetNotification().Title ?? "New Order",
                    message.GetNotification().Body ?? "");
            }
            else if (message?.Data != null && message.Data.Count > 0)
            {
                var title = "New Order";
                var body = "";
                if (message.Data.TryGetValue("title", out var t)) title = t;
                if (message.Data.TryGetValue("body", out var b)) body = b;
                ShowNotification(title, body);
            }
        }

        private void ShowNotification(string title, string body)
        {
            var intent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? "");
            if (intent != null)
            {
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            }

            var pendingIntentFlags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingIntentFlags);

            var builder = new Notification.Builder(this, Cremory.App.Services.NotificationService.ChannelId)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent)
                .SetDefaults(NotificationDefaults.Sound | NotificationDefaults.Vibrate);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                builder.SetChannelId(Services.NotificationService.ChannelId);
            }

            var notification = builder.Build();
            var manager = (NotificationManager?)GetSystemService(Context.NotificationService);
            manager?.Notify(new Random().Next(), notification);
        }
    }
}
