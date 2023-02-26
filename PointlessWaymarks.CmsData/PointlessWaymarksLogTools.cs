using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CommonTools;
using Serilog;
using Serilog.Formatting.Compact;

namespace PointlessWaymarks.CmsData;

public static class PointlessWaymarksLogTools
{
    /// <summary>
    ///     Adds File Logging to a Serilog LoggerConfiguration with a by convention file name and
    ///     a path based in the Media Archive folder based on the current user settings
    ///     - this is generally not applicable at application
    ///     startup and shouldn't be used until UserSettings are established.
    /// </summary>
    /// <param name="toAddTo"></param>
    /// <returns></returns>
    public static LoggerConfiguration AddEventFileLogForLogFolderBasedOnCurrentSettings(
        this LoggerConfiguration toAddTo)
    {
        return toAddTo.WriteTo.File(new RenderedCompactJsonFormatter(),
            Path.Combine(UserSettingsSingleton.CurrentSettings().LocalMediaArchiveLogsDirectory().FullName,
                "PointlessWaymarksCms-EventLog-.json"), rollingInterval: RollingInterval.Day, shared: true);
    }

    /// <summary>
    ///     This returns a new basic Serilog configuration for the project - Enrichers and Console
    ///     logging are added.
    /// </summary>
    /// <returns></returns>
    public static LoggerConfiguration BasicLogConfiguration()
    {
        return new LoggerConfiguration().Enrich.WithProcessId().Enrich.WithProcessName().Enrich.WithThreadId()
            .Enrich.WithThreadName().Enrich.WithMachineName().Enrich.WithEnvironmentUserName().MinimumLevel.Verbose()
            .WriteTo.Console();
    }

    /// <summary>
    ///     Returns a new configured Event Logger. This should NOT be used until user settings are initialized so
    ///     this is generally unsuitable for use at application startup.
    /// </summary>
    /// <returns></returns>
    public static ILogger EventLogger()
    {
        return BasicLogConfiguration().AddEventFileLogForLogFolderBasedOnCurrentSettings().CreateLogger();
    }

    /// <summary>
    ///     If possible calls CloseAndFlush on the current static Serilog logger and sets the static
    ///     instance to an Event Logger. This should NOT be used until user settings are initialized so
    ///     this is generally unsuitable for use at application startup.
    /// </summary>
    public static void InitializeStaticLoggerAsEventLogger()
    {
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
    ///     If possible calls CloseAndFlush on the current static Serilog logger and sets the static
    ///     instance to a Startup Logger. This will write to the program directory (not into a project
    ///     directory and is useful at application startup or for testing.
    /// </summary>
    public static void InitializeStaticLoggerAsStartupLogger()
    {
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
    ///     A helper method to log a GenerationReturn - success or failure.
    /// </summary>
    /// <param name="toLog">Appended to the 'messageTemplate'</param>
    /// <param name="logMessage"></param>
    public static void LogGenerationReturn(GenerationReturn toLog, string logMessage)
    {
        if (toLog.HasError)
        {
            Log.ForContext("Generation Return Object", toLog.SafeObjectDump())
                .ForContext("Generation Exception", toLog.Exception?.ToString())
                .Error($"Generation Return with Error - {logMessage}");
            return;
        }

        Log.ForContext("Generation Return Object", toLog.SafeObjectDump())
            .ForContext("Generation Exception", toLog.Exception?.ToString())
            .Error($"Generation Return - {logMessage}");
    }


    /// <summary>
    ///     Returns a new configured Startup Logger. This should NOT be used until user settings are initialized so
    ///     this is generally unsuitable for use at application startup.
    /// </summary>
    /// <returns></returns>
    public static ILogger StartupLogger()
    {
        return BasicLogConfiguration().LogToFileInDefaultLogDirectory("PointlessWaymarksStartupLog").CreateLogger();
    }
}