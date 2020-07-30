using System.Linq;
using System.Threading.Tasks;
using HtmlTableHelper;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsWpfControls.HtmlViewer;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.WpfHtml;

namespace PointlessWaymarksCmsWpfControls.Diagnostics
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
            reportWindow.Show();
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
            reportWindow.Show();
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
            reportWindow.Show();
        }
    }
}