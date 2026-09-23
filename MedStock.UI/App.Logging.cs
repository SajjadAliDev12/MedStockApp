using System;
using System.Threading.Tasks;
using MedStock.Services.Implementations;

namespace MedStock.UI
{
    public partial class App
    {
        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                FileLogger.Error("DispatcherUnhandled", e.Exception);
                e.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                FileLogger.Error("Domain", e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                FileLogger.Error("UnobservedTask", e.Exception);
                e.SetObserved();
            };
        }
    }
}
