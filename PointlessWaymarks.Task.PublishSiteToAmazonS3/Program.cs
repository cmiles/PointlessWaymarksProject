using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.PublishSiteToAmazonS3;
using Serilog;

LogTools.StandardStaticLoggerForProgramDirectory("PublishToS3");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.Task.PublishSiteToAmazonS3 Starting");

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
    new ToastContentBuilder()
        .AddHeader(AppDomain.CurrentDomain.FriendlyName, "Publish To S3 Failed...", new ToastArguments())
        .AddText(e.Message)
        .Show();
    Log.Error(e, "Error Running Program...");
    Console.WriteLine(e);
}

