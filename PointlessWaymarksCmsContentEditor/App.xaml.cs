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

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                EventLogContext.TryWriteExceptionToLogBlocking((Exception)e.ExceptionObject, "App.xaml.cs",
                    "AppDomain.CurrentDomain.UnhandledException");
            };

            DispatcherUnhandledException += (s, e) =>
                EventLogContext.TryWriteExceptionToLogBlocking(e.Exception, "App.xaml.cs",
                    "AppDomain.CurrentDomain.UnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
                EventLogContext.TryWriteExceptionToLogBlocking(e.Exception, "App.xaml.cs",
                    "AppDomain.CurrentDomain.UnhandledException");
        }


    }
}