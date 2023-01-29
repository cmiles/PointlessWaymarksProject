// See https://aka.ms/new-console-template for more information

using System.Reflection;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.PhotoPickup;
using Serilog;

LogTools.StandardStaticLoggerForProgramDirectory("PhotoPickup");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.Task.PhotoPickup Starting");

Console.WriteLine($"Photo Pickup - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

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

    await WindowsNotificationBuilders.NewNotifier(PhotoPickupSettings.ProgramShortName)
        .SetAutomationLogoNotificationIconUrl().SetAdditionalInformationMarkdown(FileAndFolderTools.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "README.md"))).Error(e);
}