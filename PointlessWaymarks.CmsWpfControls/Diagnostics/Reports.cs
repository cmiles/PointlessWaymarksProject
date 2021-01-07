using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlTableHelper;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.HtmlViewer;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ThreadSwitcher;
using PointlessWaymarks.CmsWpfControls.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.Diagnostics
{
    public static class Reports
    {
        public static async Task AllEventsExcelReport()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var log = await Db.Log();

            var htmlTable = log.EventLogs.OrderByDescending(x => x.RecordedOn).Cast<object>().ToList();

            ExcelHelpers.ContentToExcelFileAsTable(htmlTable, "AllEventsReport");
        }

        public static async Task AllEventsHtmlReport()
        {
            var log = await Db.Log();

            var htmlTable = log.EventLogs.Take(5000).OrderByDescending(x => x.RecordedOn).ToList()
                .ToHtmlTable(new {@class = "pure-table pure-table-striped"});

            await ThreadSwitcher.ResumeForegroundAsync();

            var reportWindow = new HtmlViewerWindow(htmlTable.ToHtmlDocumentWithPureCss("Events Report", string.Empty));
            reportWindow.PositionWindowAndShow();
        }

        public static async Task DiagnosticEventsExcelReport()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var log = await Db.Log();

            var htmlTable = log.EventLogs.Where(x => x.Category == "Diagnostic" || x.Category == "Startup")
                .OrderByDescending(x => x.RecordedOn).Cast<object>().ToList();

            ExcelHelpers.ContentToExcelFileAsTable(htmlTable, "DiagnosticEventsReport");
        }

        public static async Task DiagnosticEventsHtmlReport()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var log = await Db.Log();

            var htmlTable = log.EventLogs.Where(x => x.Category == "Diagnostic" || x.Category == "Startup").Take(5000)
                .OrderByDescending(x => x.RecordedOn).ToList()
                .ToHtmlTable(new {@class = "pure-table pure-table-striped"});

            await ThreadSwitcher.ResumeForegroundAsync();

            var reportWindow =
                new HtmlViewerWindow(htmlTable.ToHtmlDocumentWithPureCss("Diagnostic Events Report", string.Empty));
            reportWindow.PositionWindowAndShow();
        }

        public static async Task ExceptionEventsExcelReport()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var log = await Db.Log();

            var htmlTable = log.EventLogs.Where(x => x.Category == "Exception" || x.Category == "Startup")
                .OrderByDescending(x => x.RecordedOn).Cast<object>().ToList();

            ExcelHelpers.ContentToExcelFileAsTable(htmlTable, "ExceptionEventsReport");
        }

        public static async Task ExceptionEventsHtmlReport()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var log = await Db.Log();

            var htmlTable = log.EventLogs.Where(x => x.Category == "Exception" || x.Category == "Startup").Take(1000)
                .OrderByDescending(x => x.RecordedOn).ToList()
                .ToHtmlTable(new {@class = "pure-table pure-table-striped"});

            await ThreadSwitcher.ResumeForegroundAsync();

            var reportWindow =
                new HtmlViewerWindow(htmlTable.ToHtmlDocumentWithPureCss("Exception Events Report", string.Empty));
            reportWindow.PositionWindowAndShow();
        }

        public static async Task GenerationListHtmlReport(List<GenerationReturn> generationReturns, string title,
            string intro)
        {
            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine($"<p>{HttpUtility.HtmlEncode(intro)}</p>");
            bodyBuilder.AppendLine(generationReturns.ToHtmlTable(new {@class = "pure-table pure-table-striped"}));

            await ThreadSwitcher.ResumeForegroundAsync();

            var reportWindow =
                new HtmlViewerWindow(bodyBuilder.ToString().ToHtmlDocumentWithPureCss(title, string.Empty));
            reportWindow.PositionWindowAndShow();
        }

        public static async Task InvalidBracketCodeContentIdsHtmlReport(List<GenerationReturn> generationReturns)
        {
            await GenerationListHtmlReport(generationReturns.Where(x => x.HasError).ToList(),
                    "Bracket Codes with Invalid Content Ids",
                    "The content listed below contains one or more bracket codes where the ContentId is not valid. This can happen when deleting content and can be particularly unexpected when deleting and re-creating content...")
                .ConfigureAwait(false);
        }
    }
}