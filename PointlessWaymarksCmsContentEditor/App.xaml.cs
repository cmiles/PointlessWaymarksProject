using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Jot;
using Jot.Storage;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace PointlessWaymarksCmsContentEditor
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Tracker Tracker;

        public App()
        {
            Tracker = new Tracker(new JsonFileStore(UserSettingsUtilities.StorageDirectory().FullName));

            Tracker.Configure<Window>()
                .Id(w => w.Name, SystemInformation.VirtualScreen.Size) // <-- include the screen resolution in the id
                .Properties(w => new
                {
                    w.Top,
                    w.Width,
                    w.Height,
                    w.Left,
                    w.WindowState
                }).PersistOn(nameof(Window.Closing)).StopTrackingOn(nameof(Window.Closing));

            Tracker.Configure<MainWindow>().Properties(x => new {x.RecentSettingsFilesNames});
#if !DEBUG
            DispatcherUnhandledException += App_DispatcherUnhandledException;
#endif
        }


        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (!HandleApplicationException(e.Exception))
                Environment.Exit(1);

            e.Handled = true;
        }

        public static bool HandleApplicationException(Exception ex)
        {
            EventLogContext.TryWriteExceptionToLogBlocking(ex, "App.xaml.cs",
                "AppDomain.CurrentDomain.UnhandledException");

            var msg = $"Something went wrong...\r\n\r\n{ex.Message}\r\n\r\n" + "The error has been logged...\r\n\r\n" +
                      "Do you want to continue?";

            var res = MessageBox.Show(msg, "PointlessWaymarksCms App Error", MessageBoxButton.YesNo,
                MessageBoxImage.Error, MessageBoxResult.Yes);


            if (res.HasFlag(MessageBoxResult.No)) return false;
            return true;
        }
    }
}