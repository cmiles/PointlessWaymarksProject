using System.Diagnostics;
using System.Runtime.InteropServices;
using XL = Microsoft.Office.Interop.Excel;

namespace PointlessWaymarks.ExcelInteropExtensions
{
    /// <summary>
    ///     Extension methods for Microsoft.Office.Interop.Excel.Application.
    /// </summary>
    public static class ApplicationExtensionMethods
    {
        /// <summary>
        ///     Brings the active window of the given Excel instance into focus.
        /// </summary>
        /// <param name="app">The application.</param>
        public static void Activate(this XL.Application app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            using var process = app.AsProcess();
            NativeMethods.BringToFront(process);
        }

        /// <summary>
        ///     Gets the Windows Process associated with the given Excel instance.
        /// </summary>
        /// <param name="app">The application.</param>
        public static Process AsProcess(this XL.Application app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            var mainWindowHandle = app.Hwnd;
            var processId = NativeMethods.ProcessIdFromWindowHandle(mainWindowHandle);
            return Process.GetProcessById(processId);
        }

        /// <summary>
        ///     Determines whether this instance is currently the topmost Excel instance.
        /// </summary>
        /// <param name="app">The application.</param>
        public static bool IsActive(this XL.Application app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            return Equals(app, app.Session().TopMost);
        }

        /// <summary>
        ///     Determines whether this instance is visible.
        /// </summary>
        /// <param name="app">The application.</param>
        public static bool IsVisible(this XL.Application app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            try
            {
                using var process = app.AsProcess();
                return app.Visible && process.IsVisible();
            }
            catch (COMException x) when (x.Message.StartsWith(
                                             "The message filter indicated that the application is busy.") ||
                                         x.Message.StartsWith("Call was rejected by callee."))
            {
                //This means the application is in a state that does not permit COM automation.
                //Often, this is due to a dialog window or right-click context menu being open.
                return false;
            }
        }

        /// <summary>
        ///     Gets an object representing the collection of all Excel instances running
        ///     in the same Windows session as the given instance.
        /// </summary>
        /// <param name="app">The application.</param>
        public static Session Session(this XL.Application app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            using var process = app.AsProcess();
            return new Session(process.SessionId);
        }

        /// <summary>
        ///     Gets a string describing the version of the given Excel instance.
        /// </summary>
        /// <param name="app">The application.</param>
        public static string VersionName(this XL.Application app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            try
            {
                var version = (int) float.Parse(app.Version);
                return version switch
                {
                    5 => "Excel 5",
                    6 => "Excel 6",
                    7 => "Excel 95",
                    8 => "Excel 97",
                    9 => "Excel 2000",
                    10 => "Excel 2002",
                    11 => "Excel 2003",
                    12 => "Excel 2007",
                    14 => "Excel 2010",
                    15 => "Excel 2013",
                    16 => "Excel 2016",
                    _ => "Excel (Unknown version)"
                };
            }
            catch
            {
                return "Excel (Unknown version)";
            }
        }
    }
}