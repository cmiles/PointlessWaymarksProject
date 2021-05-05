using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using XL = Microsoft.Office.Interop.Excel;

namespace PointlessWaymarks.ExcelInteropExtensions
{
    /// <summary>
    ///     Provides methods to get the Z coordinate the main windows of processes.
    /// </summary>
    public static class ProcessExtensionMethods
    {
        /// <summary>
        ///     Gets the Excel instance running in the given process, or null if none exists.
        /// </summary>
        /// <param name="process">The process.</param>
        public static XL.Application AsExcelApp(this Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            var handle = process.MainWindowHandle.ToInt32();
            var result = NativeMethods.AppFromMainWindowHandle(handle);
            //Debug.Assert(result != null);
            return result;
        }

        /// <summary>
        ///     Determines whether this instance is visible.
        /// </summary>
        /// <param name="process">The process.</param>
        public static bool IsVisible(this Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            return process.MainWindowHandle.ToInt32() != 0;
        }

        /// <summary>
        ///     Gets the "Z" value of the main window. Lower Z's are closer to the screen.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <returns>"Z" value of process's main window.</returns>
        public static int MainWindowZ(this Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            return NativeMethods.GetWindowZ(process.MainWindowHandle.ToInt32());
        }

        /// <summary>
        ///     Orders the sequence of processes by the "Z" value of their main window.
        /// </summary>
        /// <param name="processes">The processes.</param>
        /// <returns>Sequence of processes, ordered by the "Z" value of their main window.</returns>
        public static IEnumerable<Process> OrderByZ(this IEnumerable<Process> processes)
        {
            if (processes == null) throw new ArgumentNullException(nameof(processes));


            return processes.Select(p => new {Process = p, Z = MainWindowZ(p)})
                .Where(x => x.Z > 0) //Filter hidden instances
                .OrderBy(x => x.Z) //Sort by z value
                .Select(x => x.Process);
        }

        /// <summary>
        ///     Returns the process with the top-most main window.
        /// </summary>
        /// <param name="processes">The processes.</param>
        /// <returns>Process with top-most main window.</returns>
        public static Process TopMost(this IEnumerable<Process> processes)
        {
            if (processes == null) throw new ArgumentNullException(nameof(processes));

            return OrderByZ(processes).FirstOrDefault();
        }
    }
}