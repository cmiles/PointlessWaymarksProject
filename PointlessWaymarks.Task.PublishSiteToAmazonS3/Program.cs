using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.PublishSiteToAmazonS3;
using Serilog;
using System.Reflection;

LogTools.StandardStaticLoggerForProgramDirectory("PublishToS3");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.Task.PublishSiteToAmazonS3 Starting");

Console.WriteLine($"Publish Site To Amazon S3 - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

if (args.Length != 1)
{
    Log.Error("Please provide a settings file name...");
    return;
}

try
{
    var runner = new PublishSiteToAmazonS3();
    await runner.Publish(args[0]);
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
        .AddAttributionText("Pointless Waymarks Project - Publish To Amazon S3")
        .Show();
}

