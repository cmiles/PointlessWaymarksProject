﻿using System.Windows;
using CommandLine;

namespace PointlessWaymarks.LocalViewer
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var optionsResult = Parser.Default.ParseArguments<CommandLineOptions>(e.Args);

            var options = (optionsResult as Parsed<CommandLineOptions>)?.Value;

            new MainWindow(options?.Url, options?.Folder, options?.SiteName).Show();
        }
    }
}