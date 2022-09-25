// See https://aka.ms/new-console-template for more information

using PointlessWaymarks.Task.MemoriesEmail;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().Enrich.WithProcessId().Enrich.WithProcessName().Enrich.WithThreadId()
    .Enrich.WithThreadName().Enrich.WithMachineName().Enrich.WithEnvironmentUserName().WriteTo.Console().WriteTo.File(new RenderedCompactJsonFormatter(),
        Path.Combine(AppContext.BaseDirectory,
            "1_PointlessWaymarksCms-MemoriesEmail-.json"), rollingInterval: RollingInterval.Day, shared: true).CreateLogger();

Log.ForContext("args", args.SafeObjectDump()).Information($"Git Commit {ThisAssembly.Git.Commit} - Commit Date {ThisAssembly.Git.CommitDate} - Is Dirty {ThisAssembly.Git.IsDirty}");

if (args.Length != 1)
{
    Log.Error( "Please provide a settings file name...");
    return;
}

try
{
    var runner = new MemoriesSmtpEmailFromWeb();
    await runner.GenerateEmail(args[0]);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);
}