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
            return "https://cremory-api-bhfhhab9fsfeazcb.indonesiacentral-01.azurewebsites.net";
        }
    }
}
