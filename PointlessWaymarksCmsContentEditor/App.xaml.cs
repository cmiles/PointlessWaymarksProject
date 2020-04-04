using System;
using System.Threading.Tasks;
using System.Windows;
using PointlessWaymarksCmsData;

namespace PointlessWaymarksCmsContentEditor
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            UserSettingsUtilities.VerifyAndCreate();
            var log = Db.Log().Result;
            log.Database.EnsureCreated();
            var db = Db.Context().Result;
            db.Database.EnsureCreated();

#if !DEBUG
            DispatcherUnhandledException += App_DispatcherUnhandledException;
#endif
        }
        
        private void App_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (!HandleApplicationException(e.Exception as Exception))
                Environment.Exit(1);

            e.Handled = true;
        }
        
        public static bool HandleApplicationException(Exception ex)
        {
            EventLogContext.TryWriteExceptionToLogBlocking(ex, "App.xaml.cs",
                "AppDomain.CurrentDomain.UnhandledException");

            var msg = $"Something went wrong...\r\n\r\n{ex.Message}\r\n\r\n" +
                      "The error has been logged...\r\n\r\n" +
                      "Do you want to continue?";

            var res = MessageBox.Show(msg,"PointlessWaymarksCms App Error",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error,MessageBoxResult.Yes);


            if (res.HasFlag(MessageBoxResult.No))
            {
                return false;
            }
            return true;
        }
    }
}