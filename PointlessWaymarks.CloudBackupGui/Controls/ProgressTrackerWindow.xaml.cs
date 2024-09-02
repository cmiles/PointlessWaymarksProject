using System.Windows;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CloudBackupGui.Controls;

/// <summary>
///     Interaction logic for ProgressTrackerWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ProgressTrackerWindow
{
    public ProgressTrackerWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public string JobName { get; init; } = string.Empty;

    public ProgressTrackerContext? ProgressContext { get; set; }

    public StatusControlContext StatusContext { get; set; }

    public static async Task<ProgressTrackerWindow> CreateInstance(Guid jobPersistentId,
        string jobName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ProgressTrackerWindow { JobName = jobName };

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.ProgressContext =
            await ProgressTrackerContext.CreateInstance(window.StatusContext, jobPersistentId, jobName);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}