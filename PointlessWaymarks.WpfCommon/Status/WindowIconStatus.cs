using System.Windows.Shell;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.Status;

/// <summary>
///     Provides management for the Windows Icon TaskbarItemInfo with properties to Bind and processing
///     of requests from multiple sources.
/// </summary>
[NotifyPropertyChanged]
public partial class WindowIconStatus
{
    private readonly List<WindowIconStatusRequest> _statusList = [];
    public decimal WindowProgress { get; set; }
    public TaskbarItemProgressState WindowState { get; set; }

    public void AddRequest(WindowIconStatusRequest request)
    {
        request.RequestedOn = DateTime.Now;

        _statusList.RemoveAll(x => x.RequestedBy == request.RequestedBy);
        _statusList.Add(request);

        if (!_statusList.Any())
        {
            WindowState = TaskbarItemProgressState.None;
            return;
        }

        if (_statusList.Any(x => x.StateRequest == TaskbarItemProgressState.Error))
        {
            WindowState = TaskbarItemProgressState.Error;
            return;
        }

        if (_statusList.Any(x => x.StateRequest == TaskbarItemProgressState.Paused))
        {
            WindowState = TaskbarItemProgressState.Paused;
            return;
        }

        if (_statusList.Any(x => x.StateRequest == TaskbarItemProgressState.Normal))
        {
            var progressEntries = _statusList.Where(x => x.StateRequest == TaskbarItemProgressState.Normal).ToList();
            WindowProgress = progressEntries.Sum(x => x.Progress ?? 0) / progressEntries.Count;
            WindowState = TaskbarItemProgressState.Normal;
            return;
        }

        if (_statusList.Any(x => x.StateRequest == TaskbarItemProgressState.Indeterminate))
        {
            WindowState = TaskbarItemProgressState.Indeterminate;
            return;
        }

        WindowState = TaskbarItemProgressState.None;
    }

    public static async Task IndeterminateTask(WindowIconStatus? windowStatus, Func<Task> toRun, Guid statusContextId)
    {
        if (windowStatus == null)
        {
            await toRun();
            return;
        }

        try
        {
            windowStatus.AddRequest(
                new WindowIconStatusRequest(statusContextId, TaskbarItemProgressState.Indeterminate));
            await toRun();
        }
        finally
        {
            windowStatus.AddRequest(new WindowIconStatusRequest(statusContextId, TaskbarItemProgressState.None));
        }
    }
}

public record WindowIconStatusRequest(Guid RequestedBy, TaskbarItemProgressState StateRequest, decimal? Progress = null)
{
    public DateTime RequestedOn { get; set; }
}