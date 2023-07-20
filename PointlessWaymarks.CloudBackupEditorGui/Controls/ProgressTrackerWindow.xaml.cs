using System.Windows;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CloudBackupEditorGui.Controls;

/// <summary>
///     Interaction logic for ProgressTrackerWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class ProgressTrackerWindow : Window
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