// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Microsoft.Toolkit.Uwp.Notifications;
using PointlessWaymarks.CmsData;
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

    new ToastContentBuilder()
        .AddAppLogoOverride(new Uri(
            $"file://{Path.Combine(AppContext.BaseDirectory, "PointlessWaymarksCmsAutomationSquareLogo.png")}"))
        .AddText($"Error: {e.Message}")
        .AddToastActivationInfo(AppContext.BaseDirectory, ToastActivationType.Protocol)
        .AddAttributionText("Pointless Waymarks Project - Photo Pickup Task")
        .Show();
}
