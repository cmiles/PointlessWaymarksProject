using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using Markdig;
using Microsoft.Toolkit.Uwp.Notifications;

namespace PointlessWaymarks.CommonTools;

public class WindowsNotificationTool
{
    public string AdditionalInformationMarkdown { get; set; }
    public string Attribution { get; set; }
    public string NotificationIconUrl { get; set; }

    public async Task Error(Exception exception)
    {
        await Error(exception.Message, exception.ToString());
    }

    public async Task Error(Exception exception, string hintText)
    {
        var body = $"""
            <p>{HtmlEncoder.Default.Encode(hintText)}</p>
            <p>{HtmlEncoder.Default.Encode(exception.ToString())}</p>
            """;

        await Error(exception.Message, body, true);
    }

    public async Task Error(string summary)
    {
        await Error(summary, string.Empty, true);
    }

    public async Task Error(string summary, string body)
    {
        await Error(summary, body, true);
    }

    public async Task Error(string summary, string body,
        bool bodyIsHtml)
    {
        if (string.IsNullOrWhiteSpace(Attribution))
            Attribution = Assembly.GetEntryAssembly()?.GetName().Name ?? "Pointless Waymarks Application";

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

        if (!string.IsNullOrWhiteSpace(AdditionalInformationMarkdown))
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var additionalHtml = Markdown.ToHtml(AdditionalInformationMarkdown, pipeline);

            htmlBuilder.Append("<h1>Additional Information</h1>");
            htmlBuilder.AppendLine(additionalHtml);
        }

        var errorReportDocument =
            await htmlBuilder.ToString().ToHtmlDocumentWithMinimalCss(errorReportTitle, string.Empty);

        var uniqueName = UniqueFileTools.UniqueFile(FileLocationTools.DefaultErrorReportsDirectory(),
            $"TaskErrorReport--{frozenNow:yyyy-MM-dd-HH-mm-ss}--{SlugTools.CreateSlug(false, Attribution)}.html");

        await File.WriteAllTextAsync(uniqueName.FullName, errorReportDocument);

        new ToastContentBuilder()
            .AddAppLogoOverride(new Uri(
                $"file://{Path.Combine(AppContext.BaseDirectory, "PointlessWaymarksCmsAutomationSquareLogo.png")}"))
            .AddText($"Error: {summary}. Click for more information...")
            .AddToastActivationInfo(uniqueName.FullName, ToastActivationType.Protocol)
            .AddAttributionText(Attribution)
            .Show();
    }
}