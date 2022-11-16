﻿using PointlessWaymarks.CmsData;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PointlessWaymarks.GeoTaggingGui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            LogHelpers.InitializeStaticLoggerAsStartupLogger();
            Log.Information(
                $"Git Commit {ThisAssembly.Git.Commit} - Commit Date {ThisAssembly.Git.CommitDate} - Is Dirty {ThisAssembly.Git.IsDirty}");

            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        public static bool HandleApplicationException(Exception ex)
        {
            Log.Error(ex, "Application Reached HandleApplicationException thru App_DispatcherUnhandledException");

            var msg = $"Something went wrong...\r\n\r\n{ex.Message}\r\n\r\n" + "The error has been logged...\r\n\r\n" +
                      "Do you want to continue?";

            var res = MessageBox.Show(msg, "PointlessWaymarksCms App Error", MessageBoxButton.YesNo, MessageBoxImage.Error,
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
}