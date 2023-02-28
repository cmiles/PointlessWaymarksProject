using System.Collections;
using System.Diagnostics;
using XL = Microsoft.Office.Interop.Excel;

namespace PointlessWaymarks.ExcelInteropExtensions;

/// <summary>
///     Represents the collection of all Excel instances running in a specific Windows session.
/// </summary>
public class Session
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Session" /> class.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    public Session(int sessionId)
    {
        Debug.WriteLine("");
        Debug.WriteLine("Session.Constructor");

        SessionId = sessionId;
    }

    /// <summary>
    ///     Gets a sequence of all currently accessible Excel instances running
    ///     in the specified Windows session.
    /// </summary>
    /// <remarks>
    ///     Sequence is untyped because Application is an embedded interop type.
    ///     Elements can be safely cast to Application.
    /// </remarks>
    public IEnumerable Apps => AppsImpl;

    /// <summary>
    ///     Gets a strongly-typed sequence of all currently accessible Excel instances
    ///     running in the specified Windows session.
    /// </summary>
    private IEnumerable<XL.Application> AppsImpl =>
        Processes.Select(TryGetApp).Where(a => a != null && a.AsProcess().IsVisible()).ToArray();

    /// <summary>
    ///     Gets an instance representing the current Windows session.
    /// </summary>
    public static Session Current => new(Process.GetCurrentProcess().SessionId);

    /// <summary>Gets a sequence of all processes in this session named "EXCEL".</summary>
    private IEnumerable<Process> Processes =>
        Process.GetProcessesByName("EXCEL").Where(p => p.SessionId == SessionId);

    /// <summary>
    ///     Gets a sequence of process IDs for all currently running Excel
    ///     processes in the specified Windows session.
    /// </summary>
    public IEnumerable<int> ProcessIds => Processes.Select(p => p.Id).ToArray();

    /// <summary>
    ///     Gets a sequence of process IDs for all currently running processes
    ///     in the specified Windows session named Excel, but which can currently be
    ///     converted to Application instances.
    /// </summary>
    public IEnumerable<int> ReachableProcessIds => AppsImpl.Select(a => a.AsProcess().Id).ToArray();

    /// <summary>
    ///     Gets the session identifier.
    /// </summary>
    public int SessionId { get; }

    /// <summary>
    ///     Gets the Excel instance with the topmost window.
    ///     Returns null if no accessible instances.
    /// </summary>
    public XL.Application TopMost
    {
        get
        {
            var dict = AppsImpl.ToDictionary(a => a.AsProcess(), a => a);

            var topProcess = dict.Keys.TopMost();

            if (topProcess == null)
                return null;
            try
            {
                return dict[topProcess];
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     Gets a sequence of process IDs for all currently running processes
    ///     in the specified Windows session named Excel, but which cannot currently be
    ///     converted to Application instances.
    /// </summary>
    public IEnumerable<int> UnreachableProcessIds => ProcessIds.Except(ReachableProcessIds).ToArray();

    /// <summary>
    ///     Tries to convert the given process to an Excel instance,
    ///     but returns null if an exception is thrown.
    /// </summary>
    private static XL.Application TryGetApp(Process process)
    {
        try
        {
            return process.AsExcelApp();
        }
        catch
        {
            return null;
        }
    }
}