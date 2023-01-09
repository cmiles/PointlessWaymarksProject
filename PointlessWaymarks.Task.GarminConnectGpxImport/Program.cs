// See https://aka.ms/new-console-template for more information

using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.GarminConnectGpxImport;
using Serilog;
using System.Reflection;

LogTools.StandardStaticLoggerForProgramDirectory("GarminConnectGpxImport");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.Task.GarminConnectGpxImport Starting");

Console.WriteLine($"Garmin Connect Gpx Import - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

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