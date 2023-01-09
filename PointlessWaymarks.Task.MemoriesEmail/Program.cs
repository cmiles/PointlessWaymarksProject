// See https://aka.ms/new-console-template for more information

using PointlessWaymarks.CommonTools;
using PointlessWaymarks.Task.MemoriesEmail;
using Serilog;
using System.Reflection;
using Microsoft.Toolkit.Uwp.Notifications;

LogTools.StandardStaticLoggerForProgramDirectory("MemoriesEmail");

Log.ForContext("args", Helpers.SafeObjectDump(args)).Information(
    "PointlessWaymarks.Task.MemoriesEmail Starting");

Console.WriteLine($"Memories Email - Build {ProgramInfoTools.GetBuildDate(Assembly.GetExecutingAssembly())}");

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

    new ToastContentBuilder()
        .AddAppLogoOverride(new Uri(
            $"file://{Path.Combine(AppContext.BaseDirectory, "PointlessWaymarksCmsAutomationSquareLogo.png")}"))
        .AddText($"Error: {e.Message}")
        .AddToastActivationInfo(AppContext.BaseDirectory, ToastActivationType.Protocol)
        .AddAttributionText("Pointless Waymarks Project - Memories Email Task")
        .Show();
}