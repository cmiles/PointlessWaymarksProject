using System.Windows;
using CommandLine;

namespace PointlessWaymarks.SiteViewerGui;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        CommonTools.LogTools.StandardStaticLoggerForDefaultLogDirectory("SiteViewer");

        base.OnStartup(e);

        var optionsResult = Parser.Default.ParseArguments<CommandLineOptions>(e.Args);

        var options = (optionsResult as Parsed<CommandLineOptions>)?.Value;

        new MainWindow(options?.Folder, options?.Url,  options?.SiteName, null).Show();
    }
}