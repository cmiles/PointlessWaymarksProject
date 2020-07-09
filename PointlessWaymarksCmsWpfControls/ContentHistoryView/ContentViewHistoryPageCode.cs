using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Markdig;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;

namespace PointlessWaymarksCmsWpfControls.ContentHistoryView
{
    public partial class ContentViewHistoryPage
    {
        public ContentViewHistoryPage(string pageTitle, string siteName, string contentTitle, List<string> items)
        {
            items ??= new List<string>();

            PageTitle = pageTitle ?? string.Empty;
            SiteName = siteName ?? string.Empty;
            ContentTitle = contentTitle ?? string.Empty;

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak()
                .Build();
            Items = items.Select(x => Markdown.ToHtml(x, pipeline)).ToList();
        }

        public string ContentTitle { get; set; }
        public List<string> Items { get; set; }

        public string PageTitle { get; set; }
        public string SiteName { get; set; }

        public string GenerateHtml(IProgress<string> progress)
        {
            progress?.Report($"Generating HTML - {PageTitle} - {ContentTitle}");
            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            return stringWriter.ToString();
        }

        public FileInfo WriteHtmlToTempFolder(IProgress<string> progress)
        {
            var possibleFileName = FolderFileUtility.TryMakeFilenameValid(ContentTitle);

            var possibleFile = new FileInfo(Path.Combine(UserSettingsUtilities.TempStorageDirectory().FullName,
                $"HistoricEntries-{possibleFileName}-{DateTime.Now:yyyy-MM-dd---HH-mm-ss}.htm"));

            progress?.Report($"Writing File - {possibleFile.FullName}");

            File.WriteAllText(possibleFile.FullName, GenerateHtml(progress));

            possibleFile.Refresh();

            return possibleFile;
        }

        public void WriteHtmlToTempFolderAndShow(IProgress<string> progress)
        {
            var file = WriteHtmlToTempFolder(progress);

            progress?.Report($"Opening {file.FullName}");
            var ps = new ProcessStartInfo(file.FullName) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}