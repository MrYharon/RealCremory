namespace Cremory.App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // This line activates the hamburger menu globally!
            MainPage = new AppShell();
        }
    }
}