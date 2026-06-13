namespace Cremory.App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // This sets your main page with a clean navigation header bar wrapper
            MainPage = new NavigationPage(new MainPage());
        }
    }
}