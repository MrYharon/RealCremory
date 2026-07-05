using Android.App;
using Android.Content;
using Android.OS;
using Cremory.App.Models;

namespace Cremory.App.Services
{
    public class NotificationService
    {
        public const string ChannelId = "order_notifications";
        private const string ChannelName = "Order Notifications";

        public static void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
            {
                Description = "Notifications for new orders"
            };

            var manager = (NotificationManager?)Platform.CurrentActivity?.GetSystemService(Context.NotificationService);
            manager?.CreateNotificationChannel(channel);
        }

        public void ShowOrderNotification(OrderDto order)
        {
            var context = Platform.CurrentActivity;
            if (context == null) return;

            var intent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
            if (intent != null)
            {
                intent.PutExtra("navigateTo", "Orders");
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            }

            var pendingIntentFlags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, pendingIntentFlags);

            var total = order.TotalPrice.ToString("N0");
            var source = order.Source == "Facebook" ? "Messenger" : order.Source;

            var builder = new Notification.Builder(context, ChannelId)
                .SetContentTitle($"New Order from {source}")
                .SetContentText($"{order.CustomerName} - {order.Items} - P{total}")
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                builder.SetChannelId(ChannelId);
            }

            var notification = builder.Build();
            var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            manager?.Notify(order.OrderId.GetHashCode(), notification);
        }
    }
}
