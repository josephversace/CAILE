using Microsoft.Extensions.Logging;

namespace IIM.App.Hybrid
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            // Set window title
            window.Title = "IIM - Intelligent Investigation Machine";

            // Set window size for desktop
#if WINDOWS || MACCATALYST
            window.Width = 1400;
            window.Height = 900;
            window.MinimumWidth = 1200;
            window.MinimumHeight = 700;
#endif

            return window;
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            base.OnStart();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            base.OnSleep();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            base.OnResume();
        }
    }
}