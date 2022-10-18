using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

namespace PointlessWaymarks.LoggingTools;

public static class Setup
{
    public static void ConfigureStandardStaticLogger(string fileNameFragment)
    {
        Log.Logger = new LoggerConfiguration().StandardEnrichers().LogToConsole()
            .LogToFileInProgramDirectory(fileNameFragment).CreateLogger();

        GlobalLogContext.PushProperty("GitCommit", ThisAssembly.Git.Commit);
        GlobalLogContext.PushProperty("GitIsDirty", ThisAssembly.Git.IsDirty);
    }

    public static LoggerConfiguration LogToConsole(this LoggerConfiguration toConfigure)
    {
        return new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console();
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
}