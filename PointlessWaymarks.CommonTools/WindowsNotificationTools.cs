using System.Reflection;
using Microsoft.Toolkit.Uwp.Notifications;

namespace PointlessWaymarks.CommonTools;

public class WindowsNotificationTool
{
    public string Attribution { get; set; }
    public string NotificationIconUrl { get; set; }

    public async Task Error(Exception exception)
    {
        await Error(exception.Message, exception.ToString());
    }

    public async Task Error(string notificationMessage, string errorReportMessage)
    {
        if (string.IsNullOrWhiteSpace(Attribution))
            Attribution = Assembly.GetEntryAssembly()?.GetName().Name ?? "Pointless Waymarks Application";

        var frozenNow = DateTime.Now;

        var errorReportTitle = $"Pointless Waymarks Project Error Report: {Attribution}, {frozenNow:F}";

        var errorReportDocument =
            await errorReportMessage.ToHtmlDocumentWithPureCss("errorReportTitle", string.Empty);

        var uniqueName = UniqueFileTools.UniqueFile(FileLocationTools.DefaultErrorReportsDirectory(),
            $"ErrorReport--{frozenNow:yyyy-MM-dd-HH-mm-ss}--{SlugTools.CreateSlug(false, Attribution)}.html");

        await File.WriteAllTextAsync(uniqueName.FullName, errorReportDocument);

        new ToastContentBuilder()
            .AddAppLogoOverride(new Uri(
                $"file://{Path.Combine(AppContext.BaseDirectory, "PointlessWaymarksCmsAutomationSquareLogo.png")}"))
            .AddText($"Error: {notificationMessage}")
            .AddToastActivationInfo(uniqueName.FullName, ToastActivationType.Protocol)
            .AddAttributionText(Attribution)
            .Show();
    }
}