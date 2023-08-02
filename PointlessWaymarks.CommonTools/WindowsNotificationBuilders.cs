using System.Globalization;
using Serilog;

namespace PointlessWaymarks.CommonTools;

/// <summary>
///     Static methods to more easily build a WindowsNotificationTool
/// </summary>
public static class WindowsNotificationBuilders
{
    /// <summary>
    ///     Removes older Task Error Reports - failures to delete individual files  are logged but silent.
    /// </summary>
    /// <param name="deleteReportsMoreThanMonthsOld"></param>
    public static void CleanUpErrorReportDirectory(int deleteReportsMoreThanMonthsOld)
    {
        var reportDirectory = FileLocationTools.DefaultErrorReportsDirectory();
        var reports = reportDirectory.GetFiles("TaskErrorReport--*.html").ToList();

        var cutoffDate = DateTime.Now.AddMonths(-1 * Math.Abs(deleteReportsMoreThanMonthsOld));

        foreach (var loopReport in reports)
        {
            var fileNameParts = loopReport.Name.Split("--");
            if (fileNameParts.Length != 3) continue;

            if (DateTime.TryParseExact(fileNameParts[1], "yyyy-MM-dd-HH-mm-ss", new DateTimeFormatInfo(),
                    DateTimeStyles.None, out var reportDateTime))
                if (reportDateTime < cutoffDate)
                    try
                    {
                        loopReport.Delete();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Log.ForContext("hint",
                                "This error is silent and ignored - file operations are expected to fail occasionally - but this log is recorded in case there are persistent problems with a file.")
                            .ForContext("loopReport", loopReport.SafeObjectDump()).Error(e,
                                "Ignored Error - Error Deleting Error Report File {errorReportFile}",
                                loopReport.FullName);
                    }
        }
    }

    /// <summary>
    ///     Creates a new WindowsNotificationTool
    /// </summary>
    /// <param name="attribution"></param>
    /// <returns></returns>
    public static async Task<WindowsNotificationTool> NewNotifier(string attribution)
    {
        return (await WindowsNotificationTool.CreateInstance()).SetAttribution(attribution);
    }

    /// <summary>
    ///     Sets the 'program' shown in the Windows Notification.
    /// </summary>
    /// <param name="toEdit"></param>
    /// <param name="attribution"></param>
    /// <returns></returns>
    public static WindowsNotificationTool SetAttribution(this WindowsNotificationTool toEdit, string attribution)
    {
        toEdit.Attribution = attribution;
        return toEdit;
    }

    /// <summary>
    ///     Sets the Logo for the notification to the circular automation logo and error version of the Circular Logo
    /// </summary>
    /// <param name="toEdit"></param>
    /// <returns></returns>
    public static WindowsNotificationTool SetAutomationLogoNotificationIconUrl(this WindowsNotificationTool toEdit)
    {
        toEdit.NotificationIconSuccessUrl =
            $"file://{Path.Combine(FileLocationTools.DefaultAssetsStorageDirectory().FullName, "PointlessWaymarksCmsAutomationCircularLogo.png")}";
        toEdit.NotificationIconErrorUrl =
            $"file://{Path.Combine(FileLocationTools.DefaultAssetsStorageDirectory().FullName, "PointlessWaymarksCmsAutomationErrorCircularLogo.png")}";
        return toEdit;
    }

    /// <summary>
    ///     Sets the additional information shown in an HTML Error report - Markdown format. Often a
    ///     program's Help or Readme information can be useful to use here.
    /// </summary>
    /// <param name="toEdit"></param>
    /// <param name="errorReportAdditionalInformationMarkdown"></param>
    /// <returns></returns>
    public static WindowsNotificationTool SetErrorReportAdditionalInformationMarkdown(
        this WindowsNotificationTool toEdit,
        string errorReportAdditionalInformationMarkdown)
    {
        toEdit.ErrorReportAdditionalInformationMarkdown = errorReportAdditionalInformationMarkdown;
        return toEdit;
    }
}