using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using Markdig;
using Microsoft.Extensions.FileProviders;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;

namespace PointlessWaymarks.CommonTools;

/// <summary>
///     Helper Class for Windows Notifications. The idea is that this - probably built with the
///     extension methods in the WindowsNotificationBuilders class - will allow you to set
///     common information once and then take advantage of that for different notifications. Also
///     for Errors an HTML Notification is built and clicking the Windows Notification will
///     show the Report.
/// </summary>
public class WindowsNotificationTool
{
    public static bool WriteAssets = true;

    private WindowsNotificationTool()
    {
        this.SetAutomationLogoNotificationIconUrl();
        Task.Run(() =>
        {
            try
            {
                WindowsNotificationBuilders.CleanUpErrorReportDirectory(6);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Log.Error(e,
                    "Ignored Exception - Failure during the Error Report Folder Cleanup of older files launched from the WindowsNotificationTool constructor.");
            }
        });
    }

    /// <summary>
    ///     Determines the Program Name that appears in the Windows Notification -
    ///     the default is "Pointless Waymarks Project"
    /// </summary>
    public string Attribution { get; set; } = "Pointless Waymarks Project";

    /// <summary>
    ///     Additional information to present in an HTML Error Report - the programs
    ///     Help or Readme information might be appropriate.
    /// </summary>
    public string ErrorReportAdditionalInformationMarkdown { get; set; } = string.Empty;

    /// <summary>
    ///     Sets the Error Icon for the Windows Notification
    /// </summary>
    public string NotificationIconErrorUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Sets the Icon for the Windows Notification - this is the base, or
    /// success, version
    /// </summary>
    public string NotificationIconSuccessUrl { get; set; } = string.Empty;

    public static async Task<WindowsNotificationTool> CreateInstance()
    {
        await WriteLogosToAssetsFolder();

        return new WindowsNotificationTool();
    }

    /// <summary>
    ///     Shows a Windows Notification with an action to show an HTML Error Report.
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public async Task Error(Exception exception)
    {
        await Error(exception.Message, exception.ToString());
    }

    /// <summary>
    ///     Shows a Windows Notification with an action to show an HTML Error Report. The
    ///     Error Report will include the hintText in the body - this can be useful for
    ///     adding additional information to help provide context or information on
    ///     what you might do to fix an error.
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="hintText"></param>
    /// <returns></returns>
    public async Task Error(Exception exception, string hintText)
    {
        var body = $"""
            <p>{HtmlEncoder.Default.Encode(hintText)}</p>
            <p>{HtmlEncoder.Default.Encode(exception.ToString())}</p>
            """;

        await Error(exception.Message, body, true);
    }

    /// <summary>
    ///     Shows a Windows Notification with an action to show an HTML Error Report. With
    ///     only a Summary message the Error Report is probably most useful if AdditionalInformationMarkdown
    ///     is provided.
    /// </summary>
    /// <param name="summary"></param>
    /// <returns></returns>
    public async Task Error(string summary)
    {
        await Error(summary, string.Empty, true);
    }

    /// <summary>
    ///     Shows a Windows Notification with an action to show an HTML Error Report.
    /// </summary>
    /// <param name="summary"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public async Task Error(string summary, string body)
    {
        await Error(summary, body, true);
    }

    /// <summary>
    ///     Shows a Windows Notification with an action to show an HTML Error Report.
    /// </summary>
    /// <param name="summary"></param>
    /// <param name="body"></param>
    /// <param name="bodyIsHtml"></param>
    /// <returns></returns>
    public async Task Error(string summary, string body,
        bool bodyIsHtml)
    {
        var frozenNow = DateTime.Now;

        var errorReportTitle = $"Report: {Attribution}";

        var htmlBuilder = new StringBuilder();

        htmlBuilder.AppendLine("<h1>Pointless Waymarks Project Error Report</h1>");
        htmlBuilder.AppendLine($"<h2>{HtmlEncoder.Default.Encode(Attribution)}</h2>");
        htmlBuilder.AppendLine($"<p>{HtmlEncoder.Default.Encode(frozenNow.ToString("F"))}</p>");
        htmlBuilder.AppendLine($"<h4>{HtmlEncoder.Default.Encode(summary)}</h4>");

        if (!string.IsNullOrWhiteSpace(body))
            htmlBuilder.AppendLine(bodyIsHtml
                ? $"{body}"
                : $"<p>{HtmlEncoder.Default.Encode(body)}</p>");

        if (!string.IsNullOrWhiteSpace(ErrorReportAdditionalInformationMarkdown))
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var additionalHtml = Markdown.ToHtml(ErrorReportAdditionalInformationMarkdown, pipeline);

            htmlBuilder.Append("<h1>Additional Information</h1>");
            htmlBuilder.AppendLine(additionalHtml);
        }

        var errorReportDocument =
            await htmlBuilder.ToString().ToHtmlDocumentWithMinimalCss(errorReportTitle, string.Empty);

        var uniqueName = UniqueFileTools.UniqueFile(FileLocationTools.DefaultErrorReportsDirectory(),
            $"TaskErrorReport--{frozenNow:yyyy-MM-dd-HH-mm-ss}--{SlugTools.CreateSlug(false, Attribution)}.html");

        if (uniqueName == null)
        {
            new ToastContentBuilder()
                .AddAppLogoOverride(new Uri(NotificationIconErrorUrl))
                .AddText($"Error: {summary}. Unable to create Error Report File...")
                .AddAttributionText(Attribution)
                .Show();

            return;
        }

        await File.WriteAllTextAsync(uniqueName.FullName, errorReportDocument);

        new ToastContentBuilder()
            .AddAppLogoOverride(new Uri(NotificationIconErrorUrl))
            .AddText($"Error: {summary}. Click for more information...")
            .AddToastActivationInfo(uniqueName.FullName, ToastActivationType.Protocol)
            .AddAttributionText(Attribution)
            .Show();
    }

    /// <summary>
    ///     Shows a Windows Notification.
    /// </summary>
    /// <param name="summary"></param>
    public void Message(string summary)
    {
        new ToastContentBuilder()
            .AddAppLogoOverride(new Uri(NotificationIconSuccessUrl))
            .AddText(summary)
            .AddAttributionText(Attribution)
            .Show();
    }

    /// <summary>
    ///     Shows a Windows Notification with a Hero Image - the image is not rescaled.
    /// </summary>
    /// <param name="summary"></param>
    /// <param name="imageUrl"></param>
    public void Message(string summary, string imageUrl)
    {
        new ToastContentBuilder()
            .AddAppLogoOverride(new Uri(NotificationIconSuccessUrl))
            .AddText(summary)
            .AddAttributionText(Attribution)
            .AddHeroImage(new Uri(imageUrl))
            .Show();
    }


    /// <summary>
    ///     Shows a Windows Notification with an action to open the specified
    /// fileName based on 'protocol' - this method does not add any information
    /// or text to the message about clicking the notification...
    /// </summary>
    /// <param name="summary"></param>
    /// <param name="fileName"></param>
    public void MessageWithFile(string summary, string fileName)
    {
        new ToastContentBuilder()
            .AddAppLogoOverride(new Uri(NotificationIconSuccessUrl))
            .AddText(summary)
            .AddToastActivationInfo(fileName, ToastActivationType.Protocol)
            .AddAttributionText(Attribution)
            .Show();
    }

    /// <summary>
    ///     Writes Embedded Assets to the Assets Folder - uses a static bool to track if this has been done already
    ///     with the intent that this runs once per program execution (not rigorous but good enough).
    /// </summary>
    /// <returns></returns>
    public static async Task WriteLogosToAssetsFolder()
    {
        if (!WriteAssets) return;

        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        var siteResources = embeddedProvider.GetDirectoryContents("");

        foreach (var loopSiteResources in siteResources.Where(x => x.Name.StartsWith("Assets")))
        {
            var fileAsStream = loopSiteResources.CreateReadStream();

            var filePathStyleName = loopSiteResources.Name.StartsWith("Assets.")
                ? loopSiteResources.Name[7..]
                : loopSiteResources.Name;

            var destinationFile =
                new FileInfo(Path.Combine(FileLocationTools.DefaultAssetsStorageDirectory().FullName,
                    filePathStyleName));

            var destinationDirectory = destinationFile.Directory;
            if (destinationDirectory is { Exists: false }) destinationDirectory.Create();

            var fileStream = File.Create(destinationFile.FullName);
            fileAsStream.Seek(0, SeekOrigin.Begin);
            await fileAsStream.CopyToAsync(fileStream).ConfigureAwait(false);
            fileStream.Close();

            Log.Verbose($"Common Tools Assets - Writing {loopSiteResources.Name} to {destinationFile.FullName}");
        }

        WriteAssets = false;
    }
}