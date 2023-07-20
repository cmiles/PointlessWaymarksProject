using System.Windows;
using System.Windows.Threading;
using CommandLine;
using Serilog;

namespace PointlessWaymarks.SiteViewerGui;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        CommonTools.LogTools.StandardStaticLoggerForDefaultLogDirectory("PwSiteViewerGui");

        base.OnStartup(e);

        var optionsResult = Parser.Default.ParseArguments<CommandLineOptions>(e.Args);

        var options = (optionsResult as Parsed<CommandLineOptions>)?.Value;

        new MainWindow(options?.Folder, options?.Url,  options?.SiteName, null).Show();
        
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }
    
    public static bool HandleApplicationException(Exception ex)
    {
        Log.Error(ex, "Application Reached HandleApplicationException thru App_DispatcherUnhandledException");

        var msg = $"Something went wrong...\r\n\r\n{ex.Message}\r\n\r\n" + "The error has been logged...\r\n\r\n" +
                  "Do you want to continue?";

        var res = MessageBox.Show(msg, "Pointless Waymarks Site Viewer App Error", MessageBoxButton.YesNo,
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