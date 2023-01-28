using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using Markdig;
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

    public async Task Error(Exception exception, string helpMarkdown)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var help = Markdown.ToHtml(helpMarkdown, pipeline);

        var message = $"""
            <h4>{HtmlEncoder.Default.Encode(exception.Message)}</h4>
            <p>{HtmlEncoder.Default.Encode(exception.ToString())}</p>
            <br>
            <h1>General Help Information</h1>
            {help}
            """;
        await Error(exception.Message, message, true);
    }

    public async Task Error(string errorMessage, string helpMarkdown)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var help = Markdown.ToHtml(helpMarkdown, pipeline);

        var message = $"""
            <p>{HtmlEncoder.Default.Encode(errorMessage)}</p>
            <br>
            <h1>General Help Information</h1>
            {help}
            """;
        await Error(errorMessage, message, true);
    }


    public async Task Error(string errorMessage, string errorBody, string helpMarkdown)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var help = Markdown.ToHtml(helpMarkdown, pipeline);

        var message = $"""
            <h4>{HtmlEncoder.Default.Encode(errorMessage)}</h4>
            <p>{HtmlEncoder.Default.Encode(errorBody)}</p>
            <br>
            <h1>General Help Information</h1>
            {help}
            """;
        await Error(errorMessage, message, true);
    }

    public async Task Error(string notificationMessage, string errorReportMessage,
        bool errorReportMessageIsHtml)
    {
        if (string.IsNullOrWhiteSpace(Attribution))
            Attribution = Assembly.GetEntryAssembly()?.GetName().Name ?? "Pointless Waymarks Application";

        var frozenNow = DateTime.Now;

        var errorReportTitle = $"Report: {Attribution}";

        var htmlBuilder = new StringBuilder();

        htmlBuilder.AppendLine("<h1>Pointless Waymarks Project Error Report</h1>");
        htmlBuilder.AppendLine($"<h2>{HtmlEncoder.Default.Encode(Attribution)}</h2>");
        htmlBuilder.AppendLine($"<p>{HtmlEncoder.Default.Encode(frozenNow.ToString("F"))}</p>");
        htmlBuilder.AppendLine(errorReportMessageIsHtml
            ? $"{errorReportMessage}"
            : $"<p>{HtmlEncoder.Default.Encode(errorReportMessage)}</p>");

        var errorReportDocument =
            await htmlBuilder.ToString().ToHtmlDocumentWithMinimalCss(errorReportTitle, string.Empty);

        var uniqueName = UniqueFileTools.UniqueFile(FileLocationTools.DefaultErrorReportsDirectory(),
            $"TaskErrorReport--{frozenNow:yyyy-MM-dd-HH-mm-ss}--{SlugTools.CreateSlug(false, Attribution)}.html");

        await File.WriteAllTextAsync(uniqueName.FullName, errorReportDocument);

        new ToastContentBuilder()
            .AddAppLogoOverride(new Uri(
                $"file://{Path.Combine(AppContext.BaseDirectory, "PointlessWaymarksCmsAutomationSquareLogo.png")}"))
            .AddText($"Error: {notificationMessage}. Click for more information...")
            .AddToastActivationInfo(uniqueName.FullName, ToastActivationType.Protocol)
            .AddAttributionText(Attribution)
            .Show();
    }
}