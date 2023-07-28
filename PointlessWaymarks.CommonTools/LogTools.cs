using System.Collections;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Formatting.Compact;

namespace PointlessWaymarks.CommonTools;

public static class LogTools
{
    public static string GetCaller([CallerMemberName] string? caller = null)
    {
        return caller ?? "(none)";
    }

    public static LoggerConfiguration LogToConsole(this LoggerConfiguration toConfigure)
    {
        return toConfigure.MinimumLevel.Verbose().WriteTo.Console();
    }

    public static LoggerConfiguration LogToFileInDefaultLogDirectory(this LoggerConfiguration toConfigure,
        string fileNameFragment)
    {
        var programDirectory = FileLocationTools.DefaultLogStorageDirectory();

        return toConfigure.MinimumLevel.Verbose().WriteTo.File(new CompactJsonFormatter(),
            Path.Combine(programDirectory.FullName, $"{fileNameFragment}-Log.json"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
    }

    public static LoggerConfiguration LogToFileInProgramDirectory(this LoggerConfiguration toConfigure,
        string fileNameFragment)
    {
        var programDirectory = AppContext.BaseDirectory;

        return toConfigure.MinimumLevel.Verbose().WriteTo.File(new CompactJsonFormatter(),
            Path.Combine(programDirectory, $"1_Log-{fileNameFragment}.json"), rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
    }

    /// <summary>
    ///     Returns a simple string representation of an object with a Max Depth of 2 to avoid unexpected
    ///     problems and provide generally appropriate output for logging.
    /// </summary>
    /// <param name="toDump"></param>
    /// <returns></returns>
    public static string SafeObjectDump(this object? toDump)
    {
        return toDump is null
            ? "null"
            : ObjectDumper.Dump(toDump, new DumpOptions { MaxLevel = 2, DumpStyle = DumpStyle.Console });
    }

    public static string SafeObjectDumpNoEnumerables(this object? toDump)
    {
        if (toDump is null) return "null";

        var enumerableProperties = toDump.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(string) ||
                        typeof(IEnumerable).IsAssignableFrom(p.PropertyType)).Select(x => x.Name).ToList();

        return ObjectDumper.Dump(toDump,
            new DumpOptions { MaxLevel = 2, DumpStyle = DumpStyle.Console, ExcludeProperties = enumerableProperties });
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
            Log.Information($"Build Date {ProgramInfoTools.GetEntryAssemblyBuildDate()}");
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
            Log.Information($"{ProgramInfoTools.GetEntryAssemblyBuildDate()}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}