using System.Windows;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class BatchListWindow
{
    public BatchListWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public string JobName { get; set; } = string.Empty;

    public BatchListContext? ProgressContext { get; set; }

    public StatusControlContext StatusContext { get; set; }

    public static async Task<BatchListWindow> CreateInstance(int jobId,
        string jobName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new BatchListWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.ProgressContext =
            await BatchListContext.CreateInstance(window.StatusContext, jobId);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}