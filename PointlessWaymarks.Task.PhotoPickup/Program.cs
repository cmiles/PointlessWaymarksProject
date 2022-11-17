// See https://aka.ms/new-console-template for more information

using PointlessWaymarks.CmsData;
using PointlessWaymarks.LoggingTools;
using PointlessWaymarks.Task.PhotoPickup;
using Serilog;

LogTools.StandardStaticLoggerForProgramDirectory("PhotoPickup");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.Task.PhotoPickup Starting");

if (args.Length != 1)
{
    Log.Error("Please provide a settings file name...");
    return;
}

try
{
    var runner = new PhotoPickup();
    await runner.PickupPhotos(args[0]);
}
catch (Exception e)
{
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);
}
