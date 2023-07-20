using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CloudBackupEditorGui.Controls;

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

    public static async Task<BatchListWindow> CreateInstanceAndShow(int jobId,
        string jobName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new BatchListWindow
        {
            StatusContext =
            {
                BlockUi = true
            },
            JobName = jobName
        };

        window.PositionWindowAndShow();
        
        await ThreadSwitcher.ResumeBackgroundAsync();

        window.StatusContext.Progress("Batch List - Creating Context");

        window.ProgressContext =
            await BatchListContext.CreateInstance(window.StatusContext, jobId);

        window.StatusContext.BlockUi = false;
        
        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}