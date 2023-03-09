using System.Diagnostics;
using System.IO;
using Markdig;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsWpfControls.ContentHistoryView;

public partial class ContentViewHistoryPage
{
    public ContentViewHistoryPage(string? pageTitle, string? siteName, string? contentTitle, List<string>? items)
    {
        items ??= new List<string>();

        PageTitle = pageTitle ?? string.Empty;
        SiteName = siteName ?? string.Empty;
        ContentTitle = contentTitle ?? string.Empty;

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak()
            .Build();
        Items = items.Select(x => Markdown.ToHtml(x, pipeline)).ToList();
    }

    public string ContentTitle { get; }
    public List<string> Items { get; }
    public string PageTitle { get; }
    public string SiteName { get; }

    public string GenerateHtml(IProgress<string>? progress = null)
    {
        progress?.Report($"Generating HTML - {PageTitle} - {ContentTitle}");

        return TransformText();
    }

    public FileInfo WriteHtmlToTempFolder(IProgress<string>? progress = null)
    {
        var possibleFileName = FileAndFolderTools.TryMakeFilenameValid(ContentTitle);

        var possibleFile = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
            $"HistoricEntries-{possibleFileName}-{DateTime.Now:yyyy-MM-dd---HH-mm-ss}.htm"));

        progress?.Report($"Writing File - {possibleFile.FullName}");

        File.WriteAllText(possibleFile.FullName, GenerateHtml(progress));

        possibleFile.Refresh();

        return possibleFile;
    }

    public void WriteHtmlToTempFolderAndShow(IProgress<string>? progress = null)
    {
        var file = WriteHtmlToTempFolder(progress);

        progress?.Report($"Opening {file.FullName}");
        var ps = new ProcessStartInfo(file.FullName) {UseShellExecute = true, Verb = "open"};
        Process.Start(ps);
    }
}