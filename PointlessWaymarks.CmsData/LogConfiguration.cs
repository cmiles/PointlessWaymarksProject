#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Formatting.Compact;

namespace PointlessWaymarks.CmsData
{
    public static class LogConfiguration
    {
        /// <summary>
        /// This returns a new basic Serilog configuration for the project - Enrichers and Console
        /// logging are added.
        /// </summary>
        /// <returns></returns>
        public static LoggerConfiguration BasicLogConfiguration()
        {
            return new LoggerConfiguration()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentUserName()
                .WriteTo.Console();
        }

        /// <summary>
        /// Returns a new configured Startup Logger. This should NOT be used until user settings are initialized so
        /// this is generally unsuitable for use at application startup.
        /// </summary>
        /// <returns></returns>
        public static ILogger StartupLogger()
        {
            return BasicLogConfiguration().AddStartupFileLogForLogFolderBasedOnCurrentSettings().CreateLogger();
        }


        /// <summary>
        /// Returns a new configured Event Logger. This should NOT be used until user settings are initialized so
        /// this is generally unsuitable for use at application startup.
        /// </summary>
        /// <returns></returns>
        public static ILogger EventLogger()
        {
            return BasicLogConfiguration().AddEventFileLogForLogFolderBasedOnCurrentSettings().CreateLogger();
        }

        /// <summary>
        /// If possible calls CloseAndFlush on the current static Serilog logger and sets the static
        /// instance to a Startup Logger. This will write to the program directory (not into a project
        /// directory and is useful at application startup or for testing.
        /// </summary>
        public static void InitializeStaticLoggerAsStartupLogger()
        {
            if (Log.Logger != null)
                try
                {
                    Log.CloseAndFlush();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            Log.Logger = StartupLogger();
        }

        /// <summary>
        /// If possible calls CloseAndFlush on the current static Serilog logger and sets the static
        /// instance to an Event Logger. This should NOT be used until user settings are initialized so
        /// this is generally unsuitable for use at application startup.
        /// </summary>
        public static void InitializeStaticLoggerAsEventLogger()
        {
            if(Log.Logger != null)
                try
                {
                    Log.CloseAndFlush();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            Log.Logger = EventLogger();
        }

        /// <summary>
        /// Adds File Logging to a Serilog LoggerConfiguration with a by convention file name and
        /// a path based in the Media Archive folder based on the current user settings
        /// - this is generally not applicable at application
        /// startup and shouldn't be used until UserSettings are established.
        /// </summary>
        /// <param name="toAddTo"></param>
        /// <returns></returns>
        public static LoggerConfiguration AddEventFileLogForLogFolderBasedOnCurrentSettings(this LoggerConfiguration toAddTo)
        {
            return toAddTo.WriteTo.File(new RenderedCompactJsonFormatter(), Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveLogsDirectory().FullName,
                "PointlessWaymarksCms-EventLog-.json"), rollingInterval: RollingInterval.Day, shared: true);
        }

        /// <summary>
        /// Adds File Logging to a Serilog LoggerConfiguration with a by convention file name into the
        /// current program directory. This is useful at the startup of the application (before UserSettings
        /// are created or loaded) or for testing.
        /// </summary>
        /// <param name="toAddTo"></param>
        /// <returns></returns>
        public static LoggerConfiguration AddStartupFileLogForLogFolderBasedOnCurrentSettings(this LoggerConfiguration toAddTo)
        {
            return toAddTo.WriteTo.File(new RenderedCompactJsonFormatter(), "PointlessWaymarksStartupLog-.txt",
                rollingInterval: RollingInterval.Day, shared: true);
        }

    }
}
