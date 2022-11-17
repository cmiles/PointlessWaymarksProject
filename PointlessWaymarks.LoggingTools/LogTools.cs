using System.Reflection;
using Serilog;
using Serilog.Formatting.Compact;

namespace PointlessWaymarks.LoggingTools;

public static class LogTools
{
    public static LoggerConfiguration LogToConsole(this LoggerConfiguration toConfigure)
    {
        return new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console();
    }

    public static LoggerConfiguration LogToFileInDefaultLogDirectory(this LoggerConfiguration toConfigure,
        string fileNameFragment)
    {
        var programDirectory = AppContext.BaseDirectory;

        return new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.File(new CompactJsonFormatter(),
            Path.Combine(programDirectory, $"{fileNameFragment}-Log.json"), rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
    }

    public static LoggerConfiguration LogToFileInProgramDirectory(this LoggerConfiguration toConfigure,
        string fileNameFragment)
    {
        var programDirectory = AppContext.BaseDirectory;

        return new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.File(new CompactJsonFormatter(),
            Path.Combine(programDirectory, $"1_Log-{fileNameFragment}.json"), rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
    }

    public static LoggerConfiguration StandardEnrichers(this LoggerConfiguration toConfigure)
    {
        return toConfigure.Enrich.WithProcessId().Enrich.WithProcessName().Enrich.WithThreadId()
            .Enrich.WithThreadName().Enrich.WithMachineName().Enrich.WithEnvironmentUserName().Enrich
            .FromGlobalLogContext();
    }

    public static void StandardStaticLoggerForDefaultLogDirectory(string fileNameFragment)
    {
        Log.Logger = new LoggerConfiguration().StandardEnrichers().LogToConsole()
            .LogToFileInDefaultLogDirectory(fileNameFragment).CreateLogger();

        try
        {
            Log.Information(
                $"Git Commit {ThisAssembly.Git.Commit} - Commit Date {ThisAssembly.Git.CommitDate} - Is Dirty {ThisAssembly.Git.IsDirty}");
            Log.Information($"{WindowTitleTools.GetEntryAssemblyBuildDate(Assembly.GetExecutingAssembly())}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static void StandardStaticLoggerForProgramDirectory(string fileNameFragment)
    {
        Log.Logger = new LoggerConfiguration().StandardEnrichers().LogToConsole()
            .LogToFileInProgramDirectory(fileNameFragment).CreateLogger();

        try
        {
            Log.Information(
                $"Git Commit {ThisAssembly.Git.Commit} - Commit Date {ThisAssembly.Git.CommitDate} - Is Dirty {ThisAssembly.Git.IsDirty}");
            Log.Information($"{WindowTitleTools.GetEntryAssemblyBuildDate(Assembly.GetExecutingAssembly())}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}