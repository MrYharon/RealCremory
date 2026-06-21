using Microsoft.Maui.Storage;

namespace Cremory.App.Services
{
    public static class AppConfig
    {
        private const string PrefKey = "api_base_url";

        public static string ApiBaseUrl
        {
            get => Preferences.Get(PrefKey, GetDefaultUrl());
            set => Preferences.Set(PrefKey, value);
        }

        public static string ApiUrl => ApiBaseUrl.TrimEnd('/');
        public static string ApiEndpoint => $"{ApiUrl}/api/";
        public static string SignalrHub => $"{ApiUrl}/hubs/orders";

        private static string GetDefaultUrl()
        {
#if ANDROID
            return "http://10.0.2.2:5105";
#else
            return "http://localhost:5105";
#endif
        }
    }
}
