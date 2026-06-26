using Cremory.App.Services;

namespace Cremory.App
{
    public partial class App : Application
    {
        public static ApiService? ApiService { get; private set; }

        public App(ApiService apiService)
        {
            InitializeComponent();
            ApiService = apiService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
