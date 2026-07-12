using Microsoft.Extensions.Logging;
using Cremory.App.Services;

namespace Cremory.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<NotificationService>();
            builder.Services.AddSingleton<SignalRService>();

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<OrdersPage>();
            builder.Services.AddTransient<InventoryPage>();
            builder.Services.AddTransient<AnalyticsPage>();
            builder.Services.AddTransient<ProductPage>();
            builder.Services.AddTransient<RecipePage>();
            builder.Services.AddTransient<SettingsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
