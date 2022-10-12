// See https://aka.ms/new-console-template for more information

using PointlessWaymarks.CmsData;
using PointlessWaymarks.Task.GarminConnectGpxImport;
using Serilog;

LogHelpers.InitializeStaticLoggerAsStartupLogger();

Log.ForContext("args", args.SafeObjectDump()).Information(
    $"Git Commit {ThisAssembly.Git.Commit} - Commit Date {ThisAssembly.Git.CommitDate} - Is Dirty {ThisAssembly.Git.IsDirty}");

if (args.Length != 1)
{
    Console.WriteLine("Please provide a settings file name...");
    return;
}

try
{
    var runner = new GpxTrackImport();
    await runner.Import(args[0]);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);
}