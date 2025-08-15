using Microsoft.UI.Xaml;

namespace IIM.App.Hybrid.WinUI
{
    public class Program
    {
        [global::System.Runtime.InteropServices.DllImport("Microsoft.ui.xaml.dll")]
        private static extern void XamlCheckProcessRequirements();

        [global::System.STAThreadAttribute]
        static void Main(string[] args)
        {
            XamlCheckProcessRequirements();
            global::Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
    }

    public partial class App : MauiWinUIApplication
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
