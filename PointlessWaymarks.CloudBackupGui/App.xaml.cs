using System.Windows;
using System.Windows.Threading;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.CloudBackupGui;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        LogTools.StandardStaticLoggerForDefaultLogDirectory("PwCloudBackupGui");

        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    public static bool HandleApplicationException(Exception ex)
    {
        Log.Error(ex, "Application Reached HandleApplicationException thru App_DispatcherUnhandledException");

        var msg = $"Something went wrong...\r\n\r\n{ex.Message}\r\n\r\n" + "The error has been logged...\r\n\r\n" +
                  "Do you want to continue?";

        var res = MessageBox.Show(msg, "PointlessWaymarksCms App Error", MessageBoxButton.YesNo,
            MessageBoxImage.Error,
            MessageBoxResult.Yes);


        return res != MessageBoxResult.No;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (!HandleApplicationException(e.Exception))
            Environment.Exit(1);

        e.Handled = true;
    }
}