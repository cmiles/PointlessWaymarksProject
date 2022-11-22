// See https://aka.ms/new-console-template for more information

using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.MemoriesEmail;
using Serilog;

LogTools.StandardStaticLoggerForProgramDirectory("MemoriesEmail");

Log.ForContext("args", Helpers.SafeObjectDump(args)).Information(
    "PointlessWaymarks.Task.MemoriesEmail Starting");

if (args.Length != 1)
{
    Log.Error("Please provide a settings file name...");
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