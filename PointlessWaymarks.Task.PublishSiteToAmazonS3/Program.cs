using System.Reflection;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.PublishSiteToAmazonS3;
using Serilog;

LogTools.StandardStaticLoggerForProgramDirectory("PublishToS3");

Log.ForContext("args", args.SafeObjectDump()).Information(
    "PointlessWaymarks.Task.PublishSiteToAmazonS3 Starting");

Console.WriteLine(
    $"Publish Site To Amazon S3 - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

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

    await (await WindowsNotificationBuilders.NewNotifier(PublishSiteToAmazonS3Settings.ProgramShortName))
        .SetAutomationLogoNotificationIconUrl()
        .SetErrorReportAdditionalInformationMarkdown(
            FileAndFolderTools.ReadAllText(Path.Combine(AppContext.BaseDirectory, "README.md"))).Error(e);
}
finally
{
    Log.CloseAndFlush();
}